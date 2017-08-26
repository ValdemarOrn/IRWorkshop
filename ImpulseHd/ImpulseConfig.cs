using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using Newtonsoft.Json;

namespace ImpulseHd
{
	public class ImpulseConfig
	{
		public const int MaxSampleLength = 8192;

		public ImpulseConfig()
		{
			SpectrumStages = new [] { new SpectrumStage() };
			OutputStage = new OutputStage();
			Enable = true;
		}

		public string Name { get; set; }
		public string FilePath { get; set; }
		public bool Enable { get; set; }
		public bool Solo { get; set; }
		public int Samplerate { get; set; }
		public int ImpulseLength { get; set; }

		[JsonIgnore]
		public double[] RawSampleData { get; set; }

		public SpectrumStage[] SpectrumStages { get; set; }
		public OutputStage OutputStage { get; private set; }

		public void LoadSampleData()
		{
			double[] waveData;

			if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
			{
				// load pure impulse as fallback
				waveData = new[] { 1.0, 0.0, 0.0, 0.0 };
			}
			else
			{
				var format = WaveFiles.ReadWaveFormat(FilePath);
				if (format.SampleRate != Samplerate)
					throw new Exception($"Only files with the specified samplerate ({Samplerate}Hz) can currently be loaded");

				waveData = WaveFiles.ReadWaveFile(FilePath)[0];
			}

			RawSampleData = waveData.Take(MaxSampleLength).ToArray();
			RawSampleData = RawSampleData.Concat(new double[MaxSampleLength - RawSampleData.Length]).ToArray();
		}
	}

	public class OutputStage
	{
		public OutputStage()
		{
			Gain = 6 / 8.0;
			Pan = 0.5;
			HighCutLeft = 1.0;
			HighCutRight = 1.0;
			WindowMethod = 0.7;
			WindowLength = 0.0;
		}

		public double Gain { get; set; }
		public double SampleDelayL { get; set; }
		public double SampleDelayR { get; set; } // additional delay, <0 := left channel delayed more, >0 right channel delayed more
		public double Pan { get; set; }
		public bool InvertPhaseLeft { get; set; }
		public bool InvertPhaseRight { get; set; }
		public double LowCutLeft { get; set; }
		public double LowCutRight { get; set; }
		public double HighCutLeft { get; set; }
		public double HighCutRight { get; set; }

		public double WindowMethod { get; set; }
		public double WindowLength { get; set; }


		public double GainTransformed => -60 + Gain * 80;
		public int SampleDelayLTransformed => (int)(ValueTables.Get(SampleDelayL, ValueTables.Response2Dec) * 4096);
		public int SampleDelayRTransformed => (int)(ValueTables.Get(SampleDelayR, ValueTables.Response2Dec) * 4096);
		public double PanTransformed => Pan * 2 - 1;
		public double LowCutLeftTransformed => 20 + ValueTables.Get(LowCutLeft, ValueTables.Response3Oct) * 1480;
		public double LowCutRightTransformed => 20 + ValueTables.Get(LowCutRight, ValueTables.Response3Oct) * 1480;
		public double HighCutLeftTransformed => 1000 + ValueTables.Get(HighCutLeft, ValueTables.Response4Oct) * 21000;
		public double HighCutRightTransformed => 1000 + ValueTables.Get(HighCutRight, ValueTables.Response4Oct) * 21000;

		public WindowMethod WindowMethodTransformed
		{
			get
			{
				if (WindowMethod < 0.25)
					return ImpulseHd.WindowMethod.Truncate;
				if (WindowMethod < 0.5)
					return ImpulseHd.WindowMethod.Linear;
				if (WindowMethod < 0.75)
					return ImpulseHd.WindowMethod.Logarithmic;
				else
					return ImpulseHd.WindowMethod.Cosine;
			}
		}
		public double WindowLengthTransformed => ValueTables.Get(WindowLength, ValueTables.Response2Oct) * 0.5;
	}

	public class SpectrumStage
	{
		public SpectrumStage()
		{
			IsEnabled = true;
			MinimumPhase = true;
			MinFreq = 0;
			MaxFreq = 1;
			LowBlendOcts = 0;
			HighBlendOcts = 0;
			Gain = 0.6;
			DelaySamples = 0;

			GainSmoothingOctaves = 0.2;
			GainSmoothingAmount = 0.5;
			GainSmoothingMode = 0.5;

			RandomGainFiltering = 0.2;
			RandomGainSeed = 0;
			RandomGainAmount = 0.0;
			RandomSkewAmount = 0.5;
			RandomGainMode = 0.5;

			FrequencySkew = 0.5;
			PinToHighFrequency = false;

			PhaseBands = 0.5;
			PhaseBandDelayAmount = 0;
			PhaseBandFreqTrack = 0.5;
			PhaseBandSeed = 0;
		}

		// Basic settings
		public bool IsEnabled { get; set; }
		public bool MinimumPhase { get; set; }
		public double MinFreq { get; set; }
		public double MaxFreq { get; set; }
		public double LowBlendOcts { get; set; }
		public double HighBlendOcts { get; set; }
		public double Gain { get; set; }
		public double DelaySamples { get; set; }

		// Gain variation
		public double GainSmoothingOctaves { get; set; }
		public double GainSmoothingAmount { get; set; }
		public double GainSmoothingMode { get; set; }

		// Applies random gain to the each frequency band.
		public double RandomGainFiltering { get; set; }
		public double RandomGainSeed { get; set; }
		public double RandomGainAmount { get; set; }
		public double RandomSkewAmount { get; set; }
		public double RandomGainMode { get; set; }

		// Skews the freuqency bands up or down
		public double FrequencySkew { get; set; }
		public bool PinToHighFrequency { get; set; } // if true, will peg the high freq. in place rather than the low frequency

		// Splits the signal up into N bands and applies random delay (stereo widening)
		public double PhaseBands { get; set; }
		public double PhaseBandDelayAmount { get; set; }
		public double PhaseBandFreqTrack { get; set; }
		public double PhaseBandSeed { get; set; }


		// Basic settings
		public double MinFreqTransformed => ValueTables.Get(MinFreq, ValueTables.Response2Dec) * 24000;
		public double MaxFreqTransformed => ValueTables.Get(MaxFreq, ValueTables.Response2Dec) * 24000;
		public double LowBlendOctsTransformed => LowBlendOcts * 5;
		public double HighBlendOctsTransformed => HighBlendOcts * 5;
		public double GainTransformed => -60 + Gain * 100;
		public double DelaySamplesTransformed => (int)(ValueTables.Get(DelaySamples, ValueTables.Response2Dec) * 4096);

		// Gain variation
		public double GainSmoothingOctavesTransformed => ValueTables.Get(GainSmoothingOctaves, ValueTables.Response2Dec) * 2;
		public double GainSmoothingAmountTransformed => (Math.Pow(10, GainSmoothingAmount * 2 - 1) - 0.1) / 0.9;
		public ApplyMode GainSmoothingModeTransformed
		{
			get
			{
				if (GainSmoothingMode < 0.33) return ApplyMode.Reduce;
				if (GainSmoothingMode < 0.66) return ApplyMode.Bipolar;
				else return ApplyMode.Amplify;
			}
		}

		// Applies random gain to the each frequency band.
		public int RandomGainFilteringTransformed => (int)(ValueTables.Get(RandomGainFiltering, ValueTables.Response2Oct) * 128);
		public int RandomGainSeedTransformed => (int)(RandomGainSeed * 10000);
		public double RandomGainAmountTransformed => RandomGainAmount * 40;
		public double RandomSkewAmountTransformed => Math.Pow(3, RandomSkewAmount * 2 - 1);
		public ApplyMode RandomGainModeTransformed
		{
			get
			{
				if (RandomGainMode < 0.33) return ApplyMode.Reduce;
				if (RandomGainMode < 0.66) return ApplyMode.Bipolar;
				else return ApplyMode.Amplify;
			}
		}

		// Skews the freuqency bands up or down
		public double FrequencySkewTransformed => Math.Pow(2, FrequencySkew * 4 - 2);

		// Splits the signal up into N bands and applies random delay (stereo widening)
		public int PhaseBandsTransformed => (int)((PhaseBands - 0.001) * 9) + 2;
		public double PhaseBandDelayAmountTransformed => (int)(ValueTables.Get(PhaseBandDelayAmount, ValueTables.Response2Dec) * 4096);
		public double PhaseBandFreqTrackTransformed => 2 * PhaseBandFreqTrack - 1;
		public int PhaseBandSeedTransformed => (int)(PhaseBandSeed * 10000);
		
	}

	public enum WindowMethod
	{
		Truncate,
		Linear,
		Logarithmic,
		Cosine,
	}

	public enum FreqSkewMode
	{
		Skew, // Gradually shifts bands up or down from the middle of the band range
		Zero, // If skew < 1, leaves "empty" bands at the top or bottom
		Move, // instead of stretching, moves the entire range up or down
	}

	public enum ApplyMode
	{
		Reduce = -1,
		Bipolar = 0,
		Amplify = 1
	};
}

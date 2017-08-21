using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;

namespace ImpulseHd
{
	public class ImpulseConfig
	{
		public const int MaxSampleLength = 8192;

		public ImpulseConfig()
		{
			SpectrumStages = new SpectrumStage[0];
			OutputStage = new OutputStage();
		}

		public string Name { get; set; }
		public string FilePath { get; set; }
		public double Samplerate { get; set; }

		public double[] RawSampleData { get; set; }

		public SpectrumStage[] SpectrumStages { get; set; }
		public OutputStage OutputStage { get; private set; }

		public void LoadSampleData()
		{
			var format = WaveFiles.ReadWaveFormat(FilePath);
			if (format.SampleRate != 48000)
				throw new Exception("Only 48Khz files supported currently");

			Samplerate = format.SampleRate;
			var waveData = WaveFiles.ReadWaveFile(FilePath)[0];
			//waveData = new[] {1.0, 0.0, 0.0, 0.0};
			RawSampleData = waveData.Take(MaxSampleLength).ToArray();
			RawSampleData = RawSampleData.Concat(new double[MaxSampleLength - RawSampleData.Length]).ToArray();
		}
	}

	public class OutputStage
	{
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

		public WindowMethod WindowMethod { get; set; }
		public double WindowLength { get; set; }
	}

	public class SpectrumStage
	{
		// Basic settings
		public bool IsEnabled { get; set; }
		public bool Solo { get; set; } // mutes out all frequency bands not in range
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
		public double FrequencySkewMode { get; set; }
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
		public FreqSkewMode FrequencySkewModeTransformed
		{
			get
			{
				if (FrequencySkewMode < 0.33) return FreqSkewMode.Move;
				if (FrequencySkewMode < 0.66) return FreqSkewMode.Skew;
				else return FreqSkewMode.Zero;
			}

		}

		// Splits the signal up into N bands and applies random delay (stereo widening)
		public int PhaseBandsTransformed => (int)((PhaseBands - 0.001) * 8) + 1;
		public double PhaseBandDelayAmountTransformed => (int)(ValueTables.Get(PhaseBandDelayAmount, ValueTables.Response2Dec) * 4096);
		public double PhaseBandFreqTrackTransformed => 2 * PhaseBandFreqTrack - 1;
		public int PhaseBandSeedTransformed => (int)(PhaseBandSeed * 10000);


		public static SpectrumStage GetDefaultStage()
		{
			return new SpectrumStage
			{
				IsEnabled = true,
				MinFreq = 0,
				MaxFreq = 1,
				LowBlendOcts = 0,
				HighBlendOcts = 0,
				Gain = 0.6,
				DelaySamples = 0,

				GainSmoothingOctaves = 0.2,
				GainSmoothingAmount = 0.5,
				GainSmoothingMode = 0.5,

				RandomGainFiltering = 0.2,
				RandomGainSeed = 0,
				RandomGainAmount = 0.0,
				RandomSkewAmount = 0.5,
				RandomGainMode = 0.5,

				FrequencySkew = 0.5,
				FrequencySkewMode = 1,
				PinToHighFrequency = false,
				
				PhaseBands = 0.5,
				PhaseBandDelayAmount = 0,
				PhaseBandFreqTrack = 0.5,
				PhaseBandSeed = 0,
			};
		}
	}

	public enum WindowMethod
	{
		Truncate,
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

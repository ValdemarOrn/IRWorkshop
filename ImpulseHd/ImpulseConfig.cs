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
		public const int MaxSampleLength = 65536;

		public ImpulseConfig()
		{
			SpectrumStages = new SpectrumStage[0];
			OutputStage = new OutputStage();
		}

		public string Name { get; set; }
		public string FilePath { get; set; }
		public double Samplerate { get; set; }

		public double[] RawSampleData { get; set; }

		public SpectrumStage[] SpectrumStages { get; private set; }
		public OutputStage OutputStage { get; private set; }

		public void LoadSampleData()
		{
			var format = WaveFiles.ReadWaveFormat(FilePath);
			if (format.SampleRate != 48000)
				throw new Exception("Only 48Khz files supported currently");

			Samplerate = format.SampleRate;
			RawSampleData = WaveFiles.ReadWaveFile(FilePath)[0].Take(MaxSampleLength).ToArray();
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
		public bool IsEnabled { get; set; }
		public bool Solo { get; set; } // mutes out all frequency bands not in range

		public double MinFreq { get; set; }
		public double MaxFreq { get; set; }
		public double CrossoverOctaves { get; set; }
		public double Gain { get; set; }
		public double DelaySamples { get; set; }

		public int GainSmoothingSamples { get; set; }
		public double GainStretch { get; set; }
		public ApplyMode GainStretchMode { get; set; } // -1, 0, +1

		// Aplpies random gain to the each frequency band.
		public int RandomGainSmoothingSamples { get; set; }
		public int RandomGainSeed { get; set; }
		public double RandomGainAmount { get; set; }
		public double RandomSkewAmount { get; set; }
		public ApplyMode RandomGainMode { get; set; }

		// Splits the signal up into N bands and applies random delay (stereo widening)
		public int PhaseBands { get; set; }
		public double PhaseBandDelayAmount { get; set; }
		public int PhaseBandSeed { get; set; }

		// Skews teh freuqency bands up or down
		public double FrequencySkew { get; set; }
		public bool PinToHighFrequency { get; set; } // if true, will peg the high freq. in place rather than the low frequency
		public FreqSkewMode FrequencySkewMode { get; set; }

		public static SpectrumStage GetDefaultStage()
		{
			return new SpectrumStage
			{
				IsEnabled = false,
				MinFreq = 0,
				MaxFreq = 24000,
				CrossoverOctaves = 0,
				Gain = 1,
				DelaySamples = 0,

				GainSmoothingSamples = 10,
				GainStretch = 0,
				GainStretchMode = ApplyMode.Bipolar,

				RandomGainSmoothingSamples = 10,
				RandomGainSeed = 0,
				RandomGainAmount = 0,
				RandomGainMode = ApplyMode.Bipolar,

				PhaseBands = 8,
				PhaseBandDelayAmount = 0,
				PhaseBandSeed = 0,

				FrequencySkew = 1,
				PinToHighFrequency = false,
				FrequencySkewMode = FreqSkewMode.Zero,
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
		Zero // If skew < 1, leaves "empty" bands at the top or bottom
	}

	public enum ApplyMode
	{
		Reduce = -1,
		Bipolar = 0,
		Amplify = 1
	};
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class ImpulseConfig
	{
		public const int SpectrumStageCount = 8;
		public const int SourceCount = 8;

		public ImpulseConfig()
		{
			SpectrumStages = Enumerable.Range(0, SpectrumStageCount).Select(x => new SpectrumStage()).ToArray();
			Sources = Enumerable.Range(0, SourceCount).Select(x => new StereoSource()).ToArray();
		}

		public string FilePath { get; set; }
		public int SampleSize { get; set; }
		public double Samplerate { get; set; }
		public bool ZeroPhase { get; set; }

		public SpectrumStage[] SpectrumStages { get; private set; }

		public double LowCut { get; set; }
		public double HighCut { get; set; }

		public StereoSource[] Sources { get; private set; }

		public double DelayLeft { get; set; }
		public double DelayRight { get; set; }
		public double GainLeft { get; set; }
		public double GainRight { get; set; }
	}

	public class StereoSource
	{
		public double Angle { get; set; }
		public double Distance { get; set; }
		public double Gain { get; set; }
		public double PhaseInvert { get; set; }
		public double LowCut { get; set; }
		public double HighCut { get; set; }
	}

	public class SpectrumStage
	{
		public bool IsEnabled { get; set; }

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

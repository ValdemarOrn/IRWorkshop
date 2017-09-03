using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using Newtonsoft.Json;

namespace IrWorkshop
{
	public class SpectrumStage
	{
		[JsonIgnore]
		public int SerializedSelectedApplySourceIndex;

		public SpectrumStage()
		{
			IsEnabled = true;
			MinimumPhase = true;
			SelectedApplySource = null;

			MinFreq = 0;
			MaxFreq = 1;
			LowBlendOcts = 0;
			HighBlendOcts = 0;
			Gain = 0.6;
			DelayMillis = 0;

			GainSmoothingOctaves = 0.2;
			GainSmoothingAmount = 0.5;
			GainSmoothingMode = 0.5;

			RandomGainFiltering = 0.2;
			RandomGainSeed = 0;
			RandomGainShift = 0;
			RandomGainAmount = 0.0;
			RandomSkewAmount = 0.5;
			RandomGainMode = 0.5;

			FrequencySkew = 0.5;
			PinToHighFrequency = false;

			PhaseBands = 0.5;
			PhaseBandDelayMillis = 0;
			PhaseBandFreqTrack = 0.5;
			PhaseBandSeed = 0;
			PhaseBandFreqShift = 0.5;
		}

		// Basic settings
		public bool IsEnabled { get; set; }
		public bool MinimumPhase { get; set; }

		[JsonIgnore]
		public ImpulseConfig SelectedApplySource { get; set; }

		public int SelectedApplySourceIndex
		{
			get => SelectedApplySource?.Index ?? -1;
			set => SerializedSelectedApplySourceIndex = value;
		}

		public double MinFreq { get; set; }
		public double MaxFreq { get; set; }
		public double LowBlendOcts { get; set; }
		public double HighBlendOcts { get; set; }
		public double Gain { get; set; }
		public double DelayMillis { get; set; }

		// Gain variation
		public double GainSmoothingOctaves { get; set; }
		public double GainSmoothingAmount { get; set; }
		public double GainSmoothingMode { get; set; }

		// Applies random gain to the each frequency band.
		public double RandomGainFiltering { get; set; }
		public double RandomGainSeed { get; set; }
		public double RandomGainShift { get; set; }
		public double RandomGainAmount { get; set; }
		public double RandomSkewAmount { get; set; }
		public double RandomGainMode { get; set; }

		// Skews the freuqency bands up or down
		public double FrequencySkew { get; set; }
		public bool PinToHighFrequency { get; set; } // if true, will peg the high freq. in place rather than the low frequency

		// Splits the signal up into N bands and applies random delay (stereo widening)
		public double PhaseBands { get; set; }
		public double PhaseBandDelayMillis { get; set; }
		public double PhaseBandFreqTrack { get; set; }
		public double PhaseBandSeed { get; set; }
		public double PhaseBandFreqShift { get; set; }

		// Basic settings
		public double MinFreqTransformed => ValueTables.Get(MinFreq, ValueTables.Response2Dec) * ImpulseConfig.MaxFrequency;
		public double MaxFreqTransformed => ValueTables.Get(MaxFreq, ValueTables.Response2Dec) * ImpulseConfig.MaxFrequency;
		public double LowBlendOctsTransformed => LowBlendOcts * 5;
		public double HighBlendOctsTransformed => HighBlendOcts * 5;
		public double GainTransformed => -60 + Gain * 100;
		public double DelayMillisTransformed => ValueTables.Get(DelayMillis, ValueTables.Response2Oct) * 80;

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
		public int RandomGainSeedTransformed => (int)(RandomGainSeed * 1000);
		public int RandomGainShiftTransformed => (int)(RandomGainShift * 1000);
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
		public double PhaseBandDelayMillisTransformed => ValueTables.Get(PhaseBandDelayMillis, ValueTables.Response2Oct) * 80;
		public double PhaseBandFreqTrackTransformed => 2 * PhaseBandFreqTrack - 1;
		public int PhaseBandSeedTransformed => (int)(PhaseBandSeed * 10000);
		public double PhaseBandFreqShiftTransformed => 0.5 + PhaseBandFreqShift;

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowProfile.Core.Ui;

namespace ImpulseHd.Ui
{
	public class SpectrumStageViewModel : ViewModelBase
	{
		private readonly SpectrumStage stage;

		public SpectrumStageViewModel(SpectrumStage stage, int index)
		{
			this.stage = stage;
			Index = index;
		}

		public int Index { get; }

		public bool IsEnabled
		{
			get { return stage.IsEnabled; }
			set { stage.IsEnabled = value; NotifyPropertyChanged(); }
		}

		public bool Solo
		{
			get { return stage.Solo; }
			set { stage.Solo = value; NotifyPropertyChanged(); }
		}

		public bool MinimumPhase
		{
			get { return stage.MinimumPhase; }
			set { stage.MinimumPhase = value; NotifyPropertyChanged(); }
		}



		public double MinFreq
		{
			get { return stage.MinFreq; }
			set { stage.MinFreq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(MinFreqReadout)); }
		}

		public double MaxFreq
		{
			get { return stage.MaxFreq; }
			set { stage.MaxFreq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(MaxFreqReadout)); }
		}

		public double LowBlendOcts
		{
			get { return stage.LowBlendOcts; }
			set { stage.LowBlendOcts = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(LowBlendOctsReadout)); }
		}

		public double HighBlendOcts
		{
			get { return stage.HighBlendOcts; }
			set { stage.HighBlendOcts = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(HighBlendOctsReadout)); }
		}

		public double Gain
		{
			get { return stage.Gain; }
			set { stage.Gain = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainReadout)); }
		}

		public double DelaySamples
		{
			get { return stage.DelaySamples; }
			set { stage.DelaySamples = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(DelaySamplesReadout)); }
		}



		public double GainSmoothingSamples
		{
			get { return stage.GainSmoothingSamples; }
			set { stage.GainSmoothingSamples = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainSmoothingSamplesReadout)); }
		}

		public double GainSmoothingAmount
		{
			get { return stage.GainSmoothingAmount; }
			set { stage.GainSmoothingAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainSmoothingAmountReadout)); }
		}

		public double GainStretchMode
		{
			get { return stage.GainStretchMode; }
			set { stage.GainStretchMode = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainStretchModeReadout)); }
		}


		public double RandomGainSmoothingSamples
		{
			get { return stage.RandomGainSmoothingSamples; }
			set { stage.RandomGainSmoothingSamples = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainSmoothingSamplesReadout)); }
		}

		public double RandomGainSeed
		{
			get { return stage.RandomGainSeed; }
			set { stage.RandomGainSeed = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainSeedReadout)); }
		}

		public double RandomGainAmount
		{
			get { return stage.RandomGainAmount; }
			set { stage.RandomGainAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainAmountReadout)); }
		}

		public double RandomSkewAmount
		{
			get { return stage.RandomSkewAmount; }
			set { stage.RandomSkewAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomSkewAmountReadout)); }
		}

		public double RandomGainMode
		{
			get { return stage.RandomGainMode; }
			set { stage.RandomGainMode = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainModeReadout)); }
		}



		public double FrequencySkew
		{
			get { return stage.FrequencySkew; }
			set { stage.FrequencySkew = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(FrequencySkewReadout)); }
		}

		public double FrequencySkewMode
		{
			get { return stage.FrequencySkewMode; }
			set { stage.FrequencySkewMode = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(FrequencySkewModeReadout)); }
		}

		public bool PinToHighFrequency
		{
			get { return stage.PinToHighFrequency; }
			set { stage.PinToHighFrequency = value; NotifyPropertyChanged(); }
		}



		public double PhaseBands
		{
			get { return stage.PhaseBands; }
			set { stage.PhaseBands = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandsReadout)); }
		}

		public double PhaseBandDelayAmount
		{
			get { return stage.PhaseBandDelayAmount; }
			set { stage.PhaseBandDelayAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandDelayAmountReadout)); }
		}

		public double PhaseBandFreqTrack
		{
			get { return stage.PhaseBandFreqTrack; }
			set { stage.PhaseBandFreqTrack = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandFreqTrackReadout)); }
		}

		public double PhaseBandSeed
		{
			get { return stage.PhaseBandSeed; }
			set { stage.PhaseBandSeed = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandSeedReadout)); }
		}

		


		public string MinFreqReadout => $"{stage.MinFreqTransformed:0}Hz";
		public string MaxFreqReadout => $"{stage.MaxFreqTransformed:0}Hz";
		public string LowBlendOctsReadout => $"{stage.LowBlendOctsTransformed:0.0} Octaves";
		public string HighBlendOctsReadout => $"{stage.HighBlendOctsTransformed:0.0} Octaves";
		public string GainReadout => $"{stage.GainTransformed:0.0}dB";
		public string DelaySamplesReadout => $"{stage.DelaySamplesTransformed:0} Samples";

		public string GainSmoothingSamplesReadout => $"{stage.GainSmoothingSamplesTransformed:0} Samples";
		public string GainSmoothingAmountReadout => $"{stage.GainSmoothingAmountTransformed:0.00}";
		public string GainStretchModeReadout => stage.GainStretchModeTransformed.ToString();

		public string RandomGainSmoothingSamplesReadout => $"{stage.RandomGainSmoothingSamplesTransformed:0} Samples";
		public string RandomGainSeedReadout => $"{stage.RandomGainSeedTransformed:0}";
		public string RandomGainAmountReadout => $"{stage.RandomGainAmountTransformed:0.0}dB";
		public string RandomSkewAmountReadout => $"{stage.RandomSkewAmountTransformed:0.00}";
		public string RandomGainModeReadout => stage.RandomGainModeTransformed.ToString();

		public string FrequencySkewReadout => $"{stage.FrequencySkewTransformed:0.00}";
		public string FrequencySkewModeReadout => stage.FrequencySkewModeTransformed.ToString();

		public string PhaseBandsReadout => $"{stage.PhaseBandsTransformed:0} Bands";
		public string PhaseBandDelayAmountReadout => $"{stage.PhaseBandDelayAmountTransformed:0} Samples";
		public string PhaseBandFreqTrackReadout => $"{stage.PhaseBandFreqTrackTransformed:0.00}";
		public string PhaseBandSeedReadout => $"{stage.PhaseBandSeedTransformed:0}";
	}
}

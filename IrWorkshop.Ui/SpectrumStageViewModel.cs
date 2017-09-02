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
		private readonly ImpulseConfig[] applySources;
		private readonly ImpulseConfig config;
		private readonly SpectrumStage stage;
		private readonly Action onUpdateCallback;

		public SpectrumStageViewModel(ImpulsePreset preset, ImpulseConfig config, SpectrumStage stage, int index, Action onUpdateCallback)
		{
			this.applySources = preset.ImpulseConfig.TakeWhile(x => x != config).OrderBy(x => x.Index).ToArray();
			applySources = new[] { new ImpulseConfig() { Name = "None", Index = -1 } }.Concat(applySources).ToArray();
			this.config = config;
			this.stage = stage;
			this.onUpdateCallback = onUpdateCallback;
			Index = index;
		}

		public int Index { get; }

		public bool IsEnabled
		{
			get { return stage.IsEnabled; }
			set { stage.IsEnabled = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public bool MinimumPhase
		{
			get { return stage.MinimumPhase; }
			set { stage.MinimumPhase = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public ImpulseConfig[] ApplySources => applySources;

		public ImpulseConfig SelectedApplySource
		{
			get { return stage.SelectedApplySource; }
			set { stage.SelectedApplySource = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}
		
		public double MinFreq
		{
			get { return stage.MinFreq; }
			set { stage.MinFreq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(MinFreqReadout)); onUpdateCallback(); }
		}

		public double MaxFreq
		{
			get { return stage.MaxFreq; }
			set { stage.MaxFreq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(MaxFreqReadout)); onUpdateCallback(); }
		}

		public double LowBlendOcts
		{
			get { return stage.LowBlendOcts; }
			set { stage.LowBlendOcts = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(LowBlendOctsReadout)); onUpdateCallback(); }
		}

		public double HighBlendOcts
		{
			get { return stage.HighBlendOcts; }
			set { stage.HighBlendOcts = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(HighBlendOctsReadout)); onUpdateCallback(); }
		}

		public double Gain
		{
			get { return stage.Gain; }
			set { stage.Gain = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainReadout)); onUpdateCallback(); }
		}

		public double DelaySamples
		{
			get { return stage.DelaySamples; }
			set { stage.DelaySamples = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(DelaySamplesReadout)); onUpdateCallback(); }
		}



		public double GainSmoothingOctaves
		{
			get { return stage.GainSmoothingOctaves; }
			set { stage.GainSmoothingOctaves = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainSmoothingOctavesReadout)); onUpdateCallback(); }
		}

		public double GainSmoothingAmount
		{
			get { return stage.GainSmoothingAmount; }
			set { stage.GainSmoothingAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainSmoothingAmountReadout)); onUpdateCallback(); }
		}

		public double GainSmoothingMode
		{
			get { return stage.GainSmoothingMode; }
			set { stage.GainSmoothingMode = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainSmoothingModeReadout)); onUpdateCallback(); }
		}


		public double RandomGainFiltering
		{
			get { return stage.RandomGainFiltering; }
			set { stage.RandomGainFiltering = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainFilteringReadout)); onUpdateCallback(); }
		}

		public double RandomGainSeed
		{
			get { return stage.RandomGainSeed; }
			set { stage.RandomGainSeed = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainSeedReadout)); onUpdateCallback(); }
		}

		public double RandomGainShift
		{
			get { return stage.RandomGainShift; }
			set { stage.RandomGainShift = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainShiftReadout)); onUpdateCallback(); }
		}

		public double RandomGainAmount
		{
			get { return stage.RandomGainAmount; }
			set { stage.RandomGainAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainAmountReadout)); onUpdateCallback(); }
		}

		public double RandomSkewAmount
		{
			get { return stage.RandomSkewAmount; }
			set { stage.RandomSkewAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomSkewAmountReadout)); onUpdateCallback(); }
		}

		public double RandomGainMode
		{
			get { return stage.RandomGainMode; }
			set { stage.RandomGainMode = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(RandomGainModeReadout)); onUpdateCallback(); }
		}



		public double FrequencySkew
		{
			get { return stage.FrequencySkew; }
			set { stage.FrequencySkew = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(FrequencySkewReadout)); onUpdateCallback(); }
		}

		public bool PinToHighFrequency
		{
			get { return stage.PinToHighFrequency; }
			set { stage.PinToHighFrequency = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}



		public double PhaseBands
		{
			get { return stage.PhaseBands; }
			set { stage.PhaseBands = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandsReadout)); onUpdateCallback(); }
		}

		public double PhaseBandDelayAmount
		{
			get { return stage.PhaseBandDelayAmount; }
			set { stage.PhaseBandDelayAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandDelayAmountReadout)); onUpdateCallback(); }
		}

		public double PhaseBandFreqTrack
		{
			get { return stage.PhaseBandFreqTrack; }
			set { stage.PhaseBandFreqTrack = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandFreqTrackReadout)); onUpdateCallback(); }
		}

		public double PhaseBandSeed
		{
			get { return stage.PhaseBandSeed; }
			set { stage.PhaseBandSeed = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandSeedReadout)); onUpdateCallback(); }
		}

		public double PhaseBandFreqShift
		{
			get { return stage.PhaseBandFreqShift; }
			set { stage.PhaseBandFreqShift = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PhaseBandFreqShiftReadout)); onUpdateCallback(); }
		}


		public string MinFreqReadout => $"{stage.MinFreqTransformed:0}Hz";
		public string MaxFreqReadout => $"{stage.MaxFreqTransformed:0}Hz";
		public string LowBlendOctsReadout => $"{stage.LowBlendOctsTransformed:0.0} Octaves";
		public string HighBlendOctsReadout => $"{stage.HighBlendOctsTransformed:0.0} Octaves";
		public string GainReadout => $"{stage.GainTransformed:0.0}dB";
		public string DelaySamplesReadout => $"{stage.DelaySamplesTransformed:0} Samples";

		public string GainSmoothingOctavesReadout => $"{stage.GainSmoothingOctavesTransformed:0.00} Octaves";
		public string GainSmoothingAmountReadout => $"{stage.GainSmoothingAmountTransformed:0.00}";
		public string GainSmoothingModeReadout => stage.GainSmoothingModeTransformed.ToString();

		public string RandomGainFilteringReadout => $"{stage.RandomGainFilteringTransformed:0} Samples";
		public string RandomGainSeedReadout => $"{stage.RandomGainSeedTransformed:0}";
		public string RandomGainShiftReadout => $"{stage.RandomGainShiftTransformed:0}";
		public string RandomGainAmountReadout => $"{stage.RandomGainAmountTransformed:0.0}dB";
		public string RandomSkewAmountReadout => $"{stage.RandomSkewAmountTransformed:0.00}";
		public string RandomGainModeReadout => stage.RandomGainModeTransformed.ToString();

		public string FrequencySkewReadout => $"{stage.FrequencySkewTransformed:0.00}";

		public string PhaseBandsReadout => $"{stage.PhaseBandsTransformed:0} Bands";
		public string PhaseBandDelayAmountReadout => $"{stage.PhaseBandDelayAmountTransformed:0} Samples";
		public string PhaseBandFreqTrackReadout => $"{stage.PhaseBandFreqTrackTransformed:0.00}";
		public string PhaseBandSeedReadout => $"{stage.PhaseBandSeedTransformed:0}";
		public string PhaseBandFreqShiftReadout => $"{stage.PhaseBandFreqShiftTransformed:0.00}x";
	}
}

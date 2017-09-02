using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowProfile.Core.Ui;

namespace ImpulseHd.Ui
{
	class OutputStageViewModel : ViewModelBase
	{
		private readonly OutputStage stage;
		private readonly Action onUpdateCallback;

		public OutputStageViewModel(OutputStage stage, Action onUpdateCallback)
		{
			this.stage = stage;
			this.onUpdateCallback = onUpdateCallback;
		}

		public double Gain
		{
			get { return stage.Gain; }
			set { stage.Gain = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(GainReadout)); onUpdateCallback(); }
		}

		public double SampleDelayL
		{
			get { return stage.SampleDelayL; }
			set { stage.SampleDelayL = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(SampleDelayLReadout)); onUpdateCallback(); }
		}

		public double SampleDelayR
		{
			get { return stage.SampleDelayR; }
			set { stage.SampleDelayR = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(SampleDelayRReadout)); onUpdateCallback(); }
		}

		public double Pan
		{
			get { return stage.Pan; }
			set { stage.Pan = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(PanReadout)); onUpdateCallback(); }
		}

		public bool InvertPhaseLeft
		{
			get { return stage.InvertPhaseLeft; }
			set { stage.InvertPhaseLeft = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public bool InvertPhaseRight
		{
			get { return stage.InvertPhaseRight; }
			set { stage.InvertPhaseRight = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public bool LowCut12dB
		{
			get { return stage.LowCut12dB; }
			set { stage.LowCut12dB = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public bool HighCut12dB
		{
			get { return stage.HighCut12dB; }
			set { stage.HighCut12dB = value; NotifyPropertyChanged(); onUpdateCallback(); }
		}

		public double LowCutLeft
		{
			get { return stage.LowCutLeft; }
			set { stage.LowCutLeft = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(LowCutLeftReadout)); onUpdateCallback(); }
		}

		public double LowCutRight
		{
			get { return stage.LowCutRight; }
			set { stage.LowCutRight = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(LowCutRightReadout)); onUpdateCallback(); }
		}

		public double HighCutLeft
		{
			get { return stage.HighCutLeft; }
			set { stage.HighCutLeft = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(HighCutLeftReadout)); onUpdateCallback(); }
		}

		public double HighCutRight
		{
			get { return stage.HighCutRight; }
			set { stage.HighCutRight = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(HighCutRightReadout)); onUpdateCallback(); }
		}
		
		public double WindowMethod
		{
			get { return stage.WindowMethod; }
			set { stage.WindowMethod = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(WindowMethodReadout)); onUpdateCallback(); }
		}

		public double WindowLength
		{
			get { return stage.WindowLength; }
			set { stage.WindowLength = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(WindowLengthReadout)); onUpdateCallback(); }
		}


		public string GainReadout => $"{stage.GainTransformed:0.00}dB";
		public string SampleDelayLReadout => $"{stage.SampleDelayLTransformed:0} Samples";
		public string SampleDelayRReadout => $"{stage.SampleDelayRTransformed:0} Samples";
		public string PanReadout => $"{stage.PanTransformed*100:0} " + (stage.PanTransformed < 0 ? "Left" : stage.PanTransformed > 0 ? "Right" : "Center");
		public string LowCutLeftReadout => $"{stage.LowCutLeftTransformed:0} Hz";
		public string LowCutRightReadout => $"{stage.LowCutRightTransformed:0} Hz";
		public string HighCutLeftReadout => $"{stage.HighCutLeftTransformed:0} Hz";
		public string HighCutRightReadout => $"{stage.HighCutRightTransformed:0} Hz";
		public string WindowMethodReadout => stage.WindowMethodTransformed.ToString();
		public string WindowLengthReadout => $"{stage.WindowLengthTransformed*100:0.00}%";
	}
}

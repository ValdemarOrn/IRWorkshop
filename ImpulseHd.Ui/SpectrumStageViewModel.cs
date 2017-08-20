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
	}
}

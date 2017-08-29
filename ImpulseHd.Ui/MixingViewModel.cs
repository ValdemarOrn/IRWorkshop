using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowProfile.Core.Ui;
using LowProfile.Visuals;
using OxyPlot;
using OxyPlot.Axes;

namespace ImpulseHd.Ui
{
	class MixingViewModel : ViewModelBase
	{
		private readonly MixingConfig mixingConfig;
		private LastRetainRateLimiter updateRateLimiter;
		private PlotModel plot1;

		public MixingViewModel(MixingConfig mixingConfig, double samplerate)
		{
			this.mixingConfig = mixingConfig;
			this.Samplerate = samplerate;
			this.updateRateLimiter = new LastRetainRateLimiter(100, UpdateInner);

			StereoEq = new ObservableCollection<double>(mixingConfig.StereoEq);
			StereoEq.CollectionChanged += (s, e) =>
			{
				mixingConfig.StereoEq[e.NewStartingIndex] = (double)e.NewItems[0];
			};

			StereoPhase = new ObservableCollection<double>(mixingConfig.StereoPhase);
			StereoPhase.CollectionChanged += (s, e) =>
			{
				mixingConfig.StereoPhase[e.NewStartingIndex] = (double)e.NewItems[0];
			};
		}

		public double Samplerate { get; set; }
		public Action OnUpdateCallback { get; set; }

		public MixingConfig MixingConfig => mixingConfig;

		public OutputStageViewModel OutputStage => new OutputStageViewModel(mixingConfig.OutputStage, Update);

		public PlotModel Plot1
		{
			get { return plot1; }
			set { plot1 = value; base.NotifyPropertyChanged(); }
		}

		// ---------------------------------- Parametric EQ Section ----------------------------------------

		public double Eq1Freq
		{
			get { return mixingConfig.Eq1Freq; }
			set { mixingConfig.Eq1Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq1FreqReadout)); Update();}
		}

		public double Eq2Freq
		{
			get { return mixingConfig.Eq2Freq; }
			set { mixingConfig.Eq2Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq2FreqReadout)); Update(); }
		}

		public double Eq3Freq
		{
			get { return mixingConfig.Eq3Freq; }
			set { mixingConfig.Eq3Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq3FreqReadout)); Update(); }
		}

		public double Eq4Freq
		{
			get { return mixingConfig.Eq4Freq; }
			set { mixingConfig.Eq4Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq4FreqReadout)); Update(); }
		}

		public double Eq5Freq
		{
			get { return mixingConfig.Eq5Freq; }
			set { mixingConfig.Eq5Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq5FreqReadout)); Update(); }
		}

		public double Eq6Freq
		{
			get { return mixingConfig.Eq6Freq; }
			set { mixingConfig.Eq6Freq = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq6FreqReadout)); Update(); }
		}

		public double Eq1Q
		{
			get { return mixingConfig.Eq1Q; }
			set { mixingConfig.Eq1Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq1QReadout)); Update(); }
		}

		public double Eq2Q
		{
			get { return mixingConfig.Eq2Q; }
			set { mixingConfig.Eq2Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq2QReadout)); Update(); }
		}

		public double Eq3Q
		{
			get { return mixingConfig.Eq3Q; }
			set { mixingConfig.Eq3Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq3QReadout)); Update(); }
		}

		public double Eq4Q
		{
			get { return mixingConfig.Eq4Q; }
			set { mixingConfig.Eq4Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq4QReadout)); Update(); }
		}

		public double Eq5Q
		{
			get { return mixingConfig.Eq5Q; }
			set { mixingConfig.Eq5Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq5QReadout)); Update(); }
		}

		public double Eq6Q
		{
			get { return mixingConfig.Eq6Q; }
			set { mixingConfig.Eq6Q = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq6QReadout)); Update(); }
		}

		public double Eq1GainDb
		{
			get { return mixingConfig.Eq1GainDb; }
			set { mixingConfig.Eq1GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq1GainDbReadout)); Update(); }
		}

		public double Eq2GainDb
		{
			get { return mixingConfig.Eq2GainDb; }
			set { mixingConfig.Eq2GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq2GainDbReadout)); Update(); }
		}

		public double Eq3GainDb
		{
			get { return mixingConfig.Eq3GainDb; }
			set { mixingConfig.Eq3GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq3GainDbReadout)); Update(); }
		}

		public double Eq4GainDb
		{
			get { return mixingConfig.Eq4GainDb; }
			set { mixingConfig.Eq4GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq4GainDbReadout)); Update(); }
		}

		public double Eq5GainDb
		{
			get { return mixingConfig.Eq5GainDb; }
			set { mixingConfig.Eq5GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq5GainDbReadout)); Update(); }
		}

		public double Eq6GainDb
		{
			get { return mixingConfig.Eq6GainDb; }
			set { mixingConfig.Eq6GainDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(Eq6GainDbReadout)); Update(); }
		}

		public string Eq1FreqReadout => $"{mixingConfig.Eq1FreqTransformed:0} Hz";
		public string Eq2FreqReadout => $"{mixingConfig.Eq2FreqTransformed:0} Hz";
		public string Eq3FreqReadout => $"{mixingConfig.Eq3FreqTransformed:0} Hz";
		public string Eq4FreqReadout => $"{mixingConfig.Eq4FreqTransformed:0} Hz";
		public string Eq5FreqReadout => $"{mixingConfig.Eq5FreqTransformed:0} Hz";
		public string Eq6FreqReadout => $"{mixingConfig.Eq6FreqTransformed:0} Hz";

		public string Eq1QReadout => $"{mixingConfig.Eq1QTransformed:0.00}";
		public string Eq2QReadout => $"{mixingConfig.Eq2QTransformed:0.00}";
		public string Eq3QReadout => $"{mixingConfig.Eq3QTransformed:0.00}";
		public string Eq4QReadout => $"{mixingConfig.Eq4QTransformed:0.00}";
		public string Eq5QReadout => $"{mixingConfig.Eq5QTransformed:0.00}";
		public string Eq6QReadout => $"{mixingConfig.Eq6QTransformed:0.00}";

		public string Eq1GainDbReadout => $"{mixingConfig.Eq1GainDbTransformed:0.0} dB";
		public string Eq2GainDbReadout => $"{mixingConfig.Eq2GainDbTransformed:0.0} dB";
		public string Eq3GainDbReadout => $"{mixingConfig.Eq3GainDbTransformed:0.0} dB";
		public string Eq4GainDbReadout => $"{mixingConfig.Eq4GainDbTransformed:0.0} dB";
		public string Eq5GainDbReadout => $"{mixingConfig.Eq5GainDbTransformed:0.0} dB";
		public string Eq6GainDbReadout => $"{mixingConfig.Eq6GainDbTransformed:0.0} dB";

		// ---------------------------- Stereo Width Section -------------------------------

		public double EqDepthDb
		{
			get { return mixingConfig.EqDepthDb; }
			set { mixingConfig.EqDepthDb = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(EqDepthDbReadout)); Update(); }
		}

		public double EqSmoothingOctaves
		{
			get { return mixingConfig.EqSmoothingOctaves; }
			set { mixingConfig.EqSmoothingOctaves = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(EqSmoothingOctavesReadout)); Update(); }
		}

		public double DelayAmount
		{
			get { return mixingConfig.DelayAmount; }
			set { mixingConfig.DelayAmount = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(DelayAmountReadout)); Update(); }
		}

		public double FreqShift
		{
			get { return mixingConfig.FreqShift; }
			set
			{
				mixingConfig.FreqShift = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(FreqShiftReadout));
				NotifyPropertyChanged(nameof(Frequencies));
				Update();
			}
		}

		public string EqDepthDbReadout => $"{mixingConfig.EqDepthDbTransformed:0.0} dB";
		public string EqSmoothingOctavesReadout => $"{mixingConfig.EqSmoothingOctavesTransformed:0.00} Octaves";
		public string DelayAmountReadout => $"{mixingConfig.DelayAmountTransformed:0} Samples";
		public string FreqShiftReadout => $"{mixingConfig.FreqShiftTransformed:0.00}x";

		public ObservableCollection<double> StereoEq { get; set; }
		public ObservableCollection<double> StereoPhase { get; set; }
		public string[] Frequencies => mixingConfig.Frequencies.ToArray();

		public void Update()
		{
			updateRateLimiter.Pulse();
		}

		private void UpdateInner()
		{
			UpdatePlots();
			OnUpdateCallback?.Invoke();
		}

		private void UpdatePlots()
		{
			var pm = new PlotModel();

			pm.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Minimum = 20 });
			pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "LeftAxis", Minimum = -20, Maximum = 20 });

			var eqProc = new EqProcessor(mixingConfig, Samplerate);
			var resp = eqProc.GetFrequencyResponse();

			var line = pm.AddLine(resp, x => 0.0, x => x.Key);
			line.Color = OxyColor.FromAColor(64, OxyColors.Black);
			line.StrokeThickness = 1.0;

			line = pm.AddLine(resp, x => AudioLib.Utils.Gain2DB(x.Value), x => x.Key);
			line.Color = OxyColors.Blue;
			line.StrokeThickness = 1.0;

			plot1 = pm;
		}
	}
}

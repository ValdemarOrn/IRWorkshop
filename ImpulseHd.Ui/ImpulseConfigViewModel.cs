using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioLib;
using LowProfile.Core.Extensions;
using LowProfile.Core.Ui;
using LowProfile.Fourier.Double;
using LowProfile.Visuals;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;

namespace ImpulseHd.Ui
{
    class ImpulseConfigViewModel : ViewModelBase
    {
	    private readonly ImpulseConfig impulseConfig;
	    private readonly LastRetainRateLimiter updateRateLimiter;
		//private OutputStage outputStage;

		private string loadSampleDirectory;
		private PlotModel plotModel;
	    private PlotModel plot2;
	    private int selectedSpectrumStage;
	    
	    public ImpulseConfigViewModel(ImpulseConfig config)
	    {
		    this.updateRateLimiter = new LastRetainRateLimiter(100, UpdateInner);
			this.impulseConfig = config;
			LoadSampleCommand = new DelegateCommand(_ => LoadSampleDialog());
		    AddStageCommand = new DelegateCommand(_ => AddStage());
		    RemoveStageCommand = new DelegateCommand(_ => RemoveStage());

		    LoadSampleData();
	    }

	    public Complex[] FftSignal { get; private set; }
	    public double[] TimeSignal { get; private set; }

		public string Name
	    {
		    get { return impulseConfig.Name; }
		    set
		    {
			    impulseConfig.Name = value;
				NotifyPropertyChanged();
		    }
	    }

	    public string FilePath
	    {
		    get { return impulseConfig.FilePath; }
		    set
		    {
			    impulseConfig.FilePath = value;
				base.NotifyPropertyChanged();
		    }
		}

	    public double Samplerate
	    {
		    get { return impulseConfig.Samplerate; }
		    set
			{
				impulseConfig.Samplerate = value;
				NotifyPropertyChanged();
			}
		}

	    public SpectrumStageViewModel[] SpectrumStages => impulseConfig.SpectrumStages.Select((x, idx) => new SpectrumStageViewModel(x, idx + 1, Update)).ToArray();

	    public int SelectedSpectrumStageIndex
		{
		    get { return selectedSpectrumStage; }
		    set { selectedSpectrumStage = value; NotifyPropertyChanged(); }
	    }

	    /*public OutputStage OutputStage
	    {
		    get { return outputStage; }
	    }*/

	    public PlotModel Plot1
	    {
		    get { return plotModel; }
		    set { plotModel = value; base.NotifyPropertyChanged(); }
	    }

	    public PlotModel Plot2
	    {
		    get { return plot2; }
		    set { plot2 = value; base.NotifyPropertyChanged(); }
	    }

	    public ICommand LoadSampleCommand { get; private set; }
		public ICommand AddStageCommand { get; private set; }
	    public ICommand RemoveStageCommand { get; private set; }

		public Action OnUpdateCallback { get; set; }
	    public Action<string> OnLoadSampleCallback { get; set; }

		public void LoadSampleData()
	    {
		    if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
			    return;

			impulseConfig.LoadSampleData();
		    Update();
		}

	    protected override void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
	    {
		    base.NotifyPropertyChanged(propertyName);
		    Update();
	    }

	    private void RemoveStage()
	    {
		    var toRemove = SelectedSpectrumStageIndex;
		    impulseConfig.SpectrumStages = impulseConfig.SpectrumStages.Take(toRemove).Concat(impulseConfig.SpectrumStages.Skip(toRemove + 1)).ToArray();
		    NotifyPropertyChanged(nameof(SpectrumStages));

			var newIndex = toRemove;
		    if (newIndex >= impulseConfig.SpectrumStages.Length)
			    newIndex--;

			SelectedSpectrumStageIndex = newIndex;
		}

	    private void AddStage()
	    {
		    impulseConfig.SpectrumStages = impulseConfig.SpectrumStages.Concat(new[] {SpectrumStage.GetDefaultStage()}).ToArray();
			NotifyPropertyChanged(nameof(SpectrumStages));
		    SelectedSpectrumStageIndex = impulseConfig.SpectrumStages.Length - 1;
		}

		private void LoadSampleDialog()
	    {
		    var openFileDialog = new OpenFileDialog();
		    openFileDialog.RestoreDirectory = true;
		    openFileDialog.InitialDirectory = loadSampleDirectory;

			if (openFileDialog.ShowDialog() == true)
		    {
			    FilePath = openFileDialog.FileName;
			    loadSampleDirectory = Path.GetDirectoryName(FilePath);
				OnLoadSampleCallback?.Invoke(FilePath);
			}

		    LoadSampleData();
	    }

		private void Update()
		{
			updateRateLimiter.Pulse();
		}

	    private void UpdateInner()
	    {
		    Console.WriteLine("{0:HH:mm:ss.fff} - Pow!", DateTime.UtcNow);
		    ComputeFft();
		    OnUpdateCallback?.Invoke();
	    }

		private void ComputeFft()
	    {
		    if (impulseConfig.RawSampleData == null)
			    return;

		    var processor = new ImpulseConfigProcessor(impulseConfig);

		    for (int i = 0; i < impulseConfig.SpectrumStages.Length; i++)
		    {
			    var stage = impulseConfig.SpectrumStages[i];
			    processor.ProcessStage(stage);
			    if (i == SelectedSpectrumStageIndex)
			    {
				    var complexSignal = processor.FftSignal;
				    var realSignal = processor.TimeSignal;
				    Plot1 = PlotFft(complexSignal);
				    Plot2 = PlotReal(realSignal);
				}
		    }

		    if (impulseConfig.SpectrumStages.Length == 0 || SelectedSpectrumStageIndex < 0)
		    {

			    var complexSignal = processor.FftSignal;
			    var realSignal = processor.TimeSignal;
			    Plot1 = PlotFft(complexSignal);
			    Plot2 = PlotReal(realSignal);

			    FftSignal = complexSignal;
			    TimeSignal = realSignal;
		    }
		    else
		    {
			    FftSignal = processor.FftSignal;
			    TimeSignal = processor.TimeSignal;
		    }
	    }

	    private PlotModel PlotReal(double[] data)
	    {
		    var sampleCount = 8192;
		    var time = sampleCount / Samplerate * 1000;
			var realData = data.Take(sampleCount).ToArray();
		    var millis = Utils.Linspace(0, time, realData.Length).ToArray();
		    var pm = new PlotModel();

			var line = pm.AddLine(millis, millis.Select(x => 0.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

			line = pm.AddLine(millis, realData);
		    line.StrokeThickness = 1.0;
			line.Color = OxyColors.Black;
			
			return pm;
		}

	    private PlotModel PlotFft(Complex[] data)
	    {
		    var magData = data.Take(data.Length / 2).Select(x => x.Abs).Select(x => Utils.Gain2DB(x)).ToArray();
		    var phaseData = data.Take(data.Length / 2).Select(x => x.Arg).ToArray();
		    //phaseData = AudioLib.Utils.UnrollPhase(phaseData);
			var hz = Utils.Linspace(0, 0.5, magData.Length).Select(x => x * Samplerate).ToArray();
			var pm = new PlotModel();
			pm.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Minimum = 10});
		    var leftAxis = new LinearAxis {Position = AxisPosition.Left, Key = "LeftAxis"};
		    var rightAxis = new LinearAxis {Position = AxisPosition.Right, Key = "RightAxis", Minimum = -Math.PI - 0.1, Maximum = Math.PI + 0.1};
			pm.Axes.Add(leftAxis);
		    pm.Axes.Add(rightAxis);

			var line = pm.AddLine(hz, hz.Select(x => 0.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

		    line = pm.AddLine(hz, hz.Select(x => -20.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

		    line = pm.AddLine(hz, hz.Select(x => -40.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

		    line = pm.AddLine(hz, hz.Select(x => -60.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

		    line = pm.AddLine(hz, phaseData);
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColors.LightGreen;
		    line.YAxisKey = "RightAxis";

			line = pm.AddLine(hz, magData);
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColors.DarkBlue;
		    line.YAxisKey = "LeftAxis";
			return pm;
		}
    }
}

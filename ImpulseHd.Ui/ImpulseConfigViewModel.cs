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
using OxyPlot.Series;

namespace ImpulseHd.Ui
{
    class ImpulseConfigViewModel : ViewModelBase
    {
	    private readonly ImpulseConfig impulseConfig;
	    private readonly LastRetainRateLimiter updateRateLimiter;

		private string loadSampleDirectory;
		private PlotModel plotModel;
	    private PlotModel plot2;
	    private int selectedSpectrumStage;
	    private bool plotImpulseBase;
	    private bool plotImpulseLeft;
	    private bool plotImpulseRight;

	    public ImpulseConfigViewModel(ImpulseConfig config)
	    {
		    this.impulseConfig = config;

			this.updateRateLimiter = new LastRetainRateLimiter(250, UpdateInner);
			LoadSampleCommand = new DelegateCommand(_ => LoadSampleDialog());
		    AddStageCommand = new DelegateCommand(_ => AddStage());
		    RemoveStageCommand = new DelegateCommand(_ => RemoveStage());
		    MoveStageLeftCommand = new DelegateCommand(_ => MoveStageLeft());
		    MoveStageRightCommand = new DelegateCommand(_ => MoveStageRight());
			PlotImpulseBase = true;
			LoadSampleData();
	    }

	    public ImpulseConfig ImpulseConfig => impulseConfig;
		
		public Complex[] PlottedFftSignal { get; private set; }
	    public double[] PlottedImpulseSignal { get; private set; }

	    public double[] ImpulseLeft { get; private set; }
	    public double[] ImpulseRight { get; private set; }

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

	    public bool Enable
	    {
		    get { return impulseConfig.Enable; }
		    set
		    {
			    impulseConfig.Enable = value;
			    base.NotifyPropertyChanged();
			    Update();
		    }
	    }

		public bool Solo
	    {
		    get { return impulseConfig.Solo; }
		    set
		    {
			    impulseConfig.Solo = value;
			    base.NotifyPropertyChanged();
			    Update();
			}
	    }

		public int Samplerate
	    {
		    get { return impulseConfig.Samplerate; }
		    set
			{
				impulseConfig.Samplerate = value;
				NotifyPropertyChanged();
			}
		}

	    public SpectrumStageViewModel[] SpectrumStages => impulseConfig.SpectrumStages.Select((x, idx) => new SpectrumStageViewModel(x, idx + 1, Update)).ToArray();

	    public OutputStageViewModel OutputStage => new OutputStageViewModel(impulseConfig.OutputStage, Update);

		public int SelectedSpectrumStageIndex
		{
		    get { return selectedSpectrumStage; }
		    set { selectedSpectrumStage = value; NotifyPropertyChanged(); }
	    }

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

	    public int ImpulseLength
		{
		    get { return impulseConfig.ImpulseLength; }
		    set { impulseConfig.ImpulseLength = value; Update(); }
	    }

	    public bool PlotImpulseBase
	    {
		    get { return plotImpulseBase; }
		    set { plotImpulseBase = value; NotifyPropertyChanged(); }
	    }

	    public bool PlotImpulseLeft
	    {
		    get { return plotImpulseLeft; }
		    set { plotImpulseLeft = value; NotifyPropertyChanged(); }
	    }

	    public bool PlotImpulseRight
	    {
		    get { return plotImpulseRight; }
		    set { plotImpulseRight = value; NotifyPropertyChanged(); }
	    }


	    public ICommand LoadSampleCommand { get; private set; }
		public ICommand AddStageCommand { get; private set; }
	    public ICommand RemoveStageCommand { get; private set; }
	    public ICommand MoveStageLeftCommand { get; private set; }
	    public ICommand MoveStageRightCommand { get; private set; }
		
		public Action OnUpdateCallback { get; set; }
	    public Action<string> OnLoadSampleCallback { get; set; }

		public void LoadSampleData()
	    {
		    if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
			    return;

			impulseConfig.LoadSampleData();
		    Update();
		}

	    public void Update()
	    {
		    updateRateLimiter.Pulse();
	    }

		protected override void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
	    {
		    base.NotifyPropertyChanged(propertyName);
		    Update();
	    }
		
	    private void MoveStageLeft()
	    {
		    var idx = SelectedSpectrumStageIndex;
			if (idx == 0)
				return;

		    var temp = impulseConfig.SpectrumStages[idx];
		    impulseConfig.SpectrumStages[idx] = impulseConfig.SpectrumStages[idx - 1];
			impulseConfig.SpectrumStages[idx - 1] = temp;
		    var selStage = SelectedSpectrumStageIndex - 1;
			NotifyPropertyChanged(nameof(SpectrumStages));
		    SelectedSpectrumStageIndex = selStage;
	    }

		private void MoveStageRight()
	    {
			var idx = SelectedSpectrumStageIndex;
		    if (idx == impulseConfig.SpectrumStages.Length - 1)
			    return;

		    var temp = impulseConfig.SpectrumStages[idx];
		    impulseConfig.SpectrumStages[idx] = impulseConfig.SpectrumStages[idx + 1];
		    impulseConfig.SpectrumStages[idx + 1] = temp;
		    var selStage = SelectedSpectrumStageIndex + 1;
			NotifyPropertyChanged(nameof(SpectrumStages));
		    SelectedSpectrumStageIndex = selStage;
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
		    impulseConfig.SpectrumStages = impulseConfig.SpectrumStages.Concat(new[] { new SpectrumStage() }).ToArray();
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
				    PlottedFftSignal = processor.FftSignal;
				    PlottedImpulseSignal = processor.TimeSignal;
				}
		    }

		    if (impulseConfig.SpectrumStages.Length == 0 || SelectedSpectrumStageIndex < 0)
		    {
			    PlottedFftSignal = processor.FftSignal;
			    PlottedImpulseSignal = processor.TimeSignal;
		    }
			
			var output = processor.ProcessOutputStage();
		    ImpulseLeft = output[0];
		    ImpulseRight = output[1];

		    Plot1 = PlotFft();
		    Plot2 = PlotReal();
		}

	    private PlotModel PlotReal()
	    {
		    var sampleCount = 8192;
		    var time = sampleCount / (double)Samplerate * 1000;
			var plottedIr = PlottedImpulseSignal.Take(sampleCount).ToArray();
		    var plottedLeft = ImpulseLeft.Take(sampleCount).ToArray();
		    var plottedRight = ImpulseRight.Take(sampleCount).ToArray();
			var millis = Utils.Linspace(0, time, plottedIr.Length).ToArray();
		    var pm = new PlotModel();

			// end of sample marker
		    var millisLine = ImpulseLength / (double)Samplerate * 1000;
		    var line = pm.AddLine(new[] { millisLine - 0.0001, millisLine + 0.0001 }, new[] { -1000.0, 1000.0 });
		    line.StrokeThickness = 2.0;
		    line.Color = OxyColor.FromArgb(50, 255, 0, 0);

			// zero line
			line = pm.AddLine(millis, millis.Select(x => 0.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

			// sample data
		    if (plotImpulseBase)
		    {
			    line = pm.AddLine(millis, plottedIr);
			    line.StrokeThickness = 1.0;
			    line.Color = OxyColors.Black;
		    }

			// left IR
		    if (PlotImpulseLeft)
		    {
			    line = pm.AddLine(millis, plottedLeft);
			    line.StrokeThickness = 1.0;
			    line.Color = OxyColors.Blue;
		    }

			// Right IR
		    if (plotImpulseRight)
		    {
			    line = pm.AddLine(millis, plottedRight);
			    line.StrokeThickness = 1.0;
			    line.Color = OxyColors.Red;
		    }

			var axis = new LinearAxis {Position = AxisPosition.Left};
		    axis.Minimum = -1;
		    axis.Maximum = 1;
			pm.Axes.Add(axis);

		    axis = new LinearAxis { Position = AxisPosition.Bottom };
		    axis.Minimum = 0;
		    axis.Maximum = millisLine * 1.1;
		    pm.Axes.Add(axis);

			return pm;
		}

	    private PlotModel PlotFft()
	    {
		    var data = PlottedFftSignal;
		    var magData = data.Take(data.Length / 2).Select(x => x.Abs).Select(x => Utils.Gain2DB(x)).ToArray();
		    var phaseData = data.Take(data.Length / 2).Select(x => x.Arg).ToArray();
		    //phaseData = AudioLib.Utils.UnrollPhase(phaseData);
			var hz = Utils.Linspace(0, 0.5, magData.Length).Select(x => x * (double)Samplerate).ToArray();

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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioLib;
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

	    //private SpectrumStage[] spectrumStages;
	    //private OutputStage outputStage;

	    private string loadSampleDirectory;
		private PlotModel plotModel;
	    private Complex[] complexOutput;
	    private Complex[] realOutput;
	    private PlotModel plot2;
	    
	    public ImpulseConfigViewModel(ImpulseConfig config)
	    {
		    this.impulseConfig = config;
			LoadSampleCommand = new DelegateCommand(_ => LoadSampleDialog());

		    LoadSampleData();
	    }
	
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

	    /*public SpectrumStage[] SpectrumStages
	    {
		    get { return spectrumStages; }
	    }

	    public OutputStage OutputStage
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
		    ComputeFft();
			OnUpdateCallback?.Invoke();
		}

	    private void ComputeFft()
	    {
		    if (impulseConfig.RawSampleData == null)
			    return;

		    var fft = new Transform(65536);
		    var values = impulseConfig.RawSampleData.Select(x => new Complex(x, 0)).ToArray();
		    realOutput = new Complex[values.Length];

			complexOutput = new Complex[values.Length];
			fft.FFT(values, complexOutput);
		    Plot1 = PlotFft(complexOutput);

			fft.IFFT(complexOutput, realOutput);
		    Plot2 = PlotReal(realOutput);
	    }

	    private PlotModel PlotReal(Complex[] data)
	    {
		    var sampleCount = 8192;
		    var time = sampleCount / Samplerate * 1000;
			var realData = data.Select(x => x.Real).Take(sampleCount).ToArray();
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
		    var hz = Utils.Linspace(0, 0.5, magData.Length).Select(x => x * Samplerate).ToArray();
			var pm = new PlotModel();
			pm.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Minimum = 10});
			var line = pm.AddLine(hz, magData);
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColors.Black;
			return pm;
		}
    }
}

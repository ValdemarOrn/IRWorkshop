using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	    private string name;
	    private string filePath;
	    private int sampleSize;
	    private double samplerate;
	    private bool zeroPhase;
	    private readonly SpectrumStage[] spectrumStages;
	    private readonly OutputStage outputStage;
	    private PlotModel plotModel;
	    private double[] rawSample;
	    private Complex[] complexOutput;
	    private Complex[] realOutput;
	    private PlotModel plot2;

	    public ImpulseConfigViewModel()
	    {
			LoadSampleCommand = new DelegateCommand(_ => LoadSample());

		}
	
	    public string Name
	    {
		    get { return name; }
		    set { name = value; NotifyPropertyChanged(); }
	    }

	    public string FilePath
	    {
		    get { return filePath; }
		    set { filePath = value; NotifyPropertyChanged(); }
		}

	    public int SampleSize
	    {
		    get { return sampleSize; }
		    set { sampleSize = value; NotifyPropertyChanged(); }
		}

	    public double Samplerate
	    {
		    get { return samplerate; }
		    set { samplerate = value; NotifyPropertyChanged(); }
		}

	    public bool ZeroPhase
	    {
		    get { return zeroPhase; }
		    set { zeroPhase = value; NotifyPropertyChanged(); }
		}

	    public SpectrumStage[] SpectrumStages
	    {
		    get { return spectrumStages; }
	    }

	    public OutputStage OutputStage
	    {
		    get { return outputStage; }
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

	    public ICommand LoadSampleCommand { get; private set; }


	    private void LoadSample()
	    {
			var openFileDialog = new OpenFileDialog();
		    if (openFileDialog.ShowDialog() == true)
			    FilePath = openFileDialog.FileName;

		    LoadSampleData();
	    }

	    public void LoadSampleData()
	    {
			var format = WaveFiles.ReadWaveFormat(FilePath);
		    if (format.SampleRate != 48000)
			    throw new Exception("Only 48Khz files supported currently");

		    Samplerate = format.SampleRate;
			this.rawSample = WaveFiles.ReadWaveFile(FilePath)[0];
		    this.rawSample = rawSample.Concat(new double[65536 - rawSample.Length]).ToArray();
		    Update();
		}

	    protected override void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
	    {
		    base.NotifyPropertyChanged(propertyName);
		    Update();
	    }

	    private void Update()
	    {
		    ComputeFft();
	    }

	    private void ComputeFft()
	    {
		    if (rawSample == null)
			    return;

		    var fft = new LowProfile.Fourier.Double.Transform(65536);
		    var values = rawSample.Select(x => new Complex(x, 0)).ToArray();
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
		    var line = pm.AddLine(millis, realData);
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

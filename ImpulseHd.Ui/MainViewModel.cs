using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioLib;
using AudioLib.PortAudioInterop;
using LowProfile.Core.Ui;
using LowProfile.Fourier.Double;
using LowProfile.Visuals;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OxyPlot;

namespace ImpulseHd.Ui
{
    class MainViewModel : ViewModelBase
    {
	    private readonly ImpulsePreset preset;
		private readonly RealtimeHost host;
	    private readonly string settingsFile;
		private readonly LastRetainRateLimiter updateRateLimiter;

		private string[] inputNames;
	    private string[] outputNames;
	    
	    private double samplerateSlider;
	    private double impulseLengthSlider;
	    private int selectedInputL;
	    private int selectedInputR;
	    private int selectedOutputL;
	    private int selectedOutputR;
	    private string loadSampleDirectory;
	    private string saveSampleDirectory;
	    private DateTime clipTimeLeft;
	    private DateTime clipTimeRight;
	    private double volumeSlider;
	    private PlotModel plotImpulseLeft;
	    private PlotModel plotImpulseRight;
	    private Brush clipLBrush;
	    private Brush clipRBrush;
	    private TabItem selectedTab;
	    private ImpulseConfigViewModel selectedImpulse;

		public MainViewModel()
		{
			Title = "CabIR Studio - v" + Assembly.GetExecutingAssembly().GetName().Version;
			settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "settings.json");
			this.updateRateLimiter = new LastRetainRateLimiter(100, UpdateSample);
			
			var ic = new ImpulseConfig() {Name = "My Impulse 123", Samplerate = 48000, FilePath = @"E:\Sound\Samples\Impulses\catharsis-awesometime-fredman\48Khz\2off-pres5.wav" };
			preset = new ImpulsePreset();
		    preset.ImpulseConfig = new[] {ic};
			ImpulseLengthSlider = 0.5;
			SamplerateSlider = 0.4;

			ImpulseConfig = new ObservableCollection<ImpulseConfigViewModel>();
			ImpulseConfig.Add(new ImpulseConfigViewModel(ic)
			{
				OnUpdateCallback = () => updateRateLimiter.Pulse(),
				OnLoadSampleCallback = dir => loadSampleDirectory = Path.GetDirectoryName(dir),
				ImpulseLength = preset.ImpulseLength,
				Samplerate = preset.Samplerate
			});
			ImpulseConfig[0].LoadSampleData();
			

			// The host lives in a singleton withing. It can not be created directly 
			// and only one host can exists within an application context
			host = RealtimeHost.Host;

			// host.Process is the callback method that processes audio data. 
			// Assign the static process method in this class to be the callback
			host.Process = ProcessAudio;
			
		    AudioSetupCommand = new DelegateCommand(_ => AudioSetup());
			ExportWavCommand = new DelegateCommand(_ => ExportWav());
			ShowAboutCommand = new DelegateCommand(_ => ShowAbout());
			CheckForUpdatesCommand = new DelegateCommand(_ => Process.Start("https://github.com/ValdemarOrn/ImpulseEngine"));

			selectedInputL = -1;
		    selectedInputR = -1;
		    selectedOutputL = -1;
		    selectedOutputR = -1;

			LoadSettings();

		    var t = new Thread(SaveSettings) {IsBackground = true};
			t.Priority = ThreadPriority.Lowest;
			t.Start();

			var t3 = new Thread(UpdateClipIndicators) { IsBackground = true };
			t3.Priority = ThreadPriority.Lowest;
			t3.Start();
		}

	    public string Title { get; set; }
		
	    public ObservableCollection<ImpulseConfigViewModel> ImpulseConfig
		{
			get;
			private set;
		}

	    public ImpulseConfigViewModel SelectedImpulse
	    {
		    get { return selectedImpulse; }
		    set
		    {
			    selectedImpulse = value;
				NotifyPropertyChanged();
			    NotifyPropertyChanged(nameof(PlotTop));
			    NotifyPropertyChanged(nameof(PlotBottom));
			}
	    }

	    public ICommand AudioSetupCommand { get; private set; }
		public ICommand ExportWavCommand { get; private set; }
		public ICommand ShowAboutCommand { get; private set; }
		public ICommand CheckForUpdatesCommand { get; private set; }

		public string[] InputNames
	    {
		    get { return inputNames; }
		    private set { inputNames = value; NotifyPropertyChanged(); }
	    }

	    public string[] OutputNames
	    {
		    get { return outputNames; }
		    private set { outputNames = value; NotifyPropertyChanged(); }
	    }

	    public double SamplerateSlider
		{
		    get { return samplerateSlider; }
		    set
			{
				samplerateSlider = value;

				if (value < 0.25)
					preset.Samplerate = 44100;
				else if (value < 0.5)
					preset.Samplerate = 48000;
				else if (value < 0.75)
					preset.Samplerate = 44100 * 2;
				else
					preset.Samplerate = 96000;

				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(SamplerateReadout));
				NotifyPropertyChanged(nameof(SamplerateWarning));
			}
	    }

	    public string SamplerateReadout => preset.Samplerate.ToString();

	    public double ImpulseLengthSlider
	    {
		    get { return impulseLengthSlider; }
		    set
		    {
			    impulseLengthSlider = value;
			    var iVal = (int)((value - 0.0001) * 5);
			    if (iVal == 0)
				    preset.ImpulseLength = 256;
			    else if (iVal == 1)
				    preset.ImpulseLength = 512;
			    else if (iVal == 2)
				    preset.ImpulseLength = 1024;
			    else if (iVal == 3)
				    preset.ImpulseLength = 2048;
			    else if (iVal == 4)
				    preset.ImpulseLength = 4096;

			    if (ImpulseConfig != null)
			    {
				    foreach (var ic in ImpulseConfig)
					    ic.ImpulseLength = preset.ImpulseLength;
			    }

			    NotifyPropertyChanged();
			    NotifyPropertyChanged(nameof(ImpulseLengthReadout));
			    updateRateLimiter.Pulse();
			}
	    }

	    public string ImpulseLengthReadout => preset.ImpulseLength + " - " + string.Format("{0:0.0}ms", preset.ImpulseLength / (double)preset.Samplerate * 1000);

	    public double VolumeSlider
	    {
		    get { return volumeSlider; }
		    set
		    {
			    volumeSlider = value;
			    NotifyPropertyChanged(nameof(VolumeReadout));
			}
	    }

	    public double VolumeDb => (VolumeSlider * 80 - 60);
		
		public string VolumeReadout => $"{VolumeDb:0.0}dB";

	    public string SamplerateWarning => (host.Config != null && host.Config.Samplerate != preset.Samplerate)
		    ? "For accurate results, impulse samplerate should match\r\naudio device samplerate"
		    : "";

	    public int SelectedInputL
	    {
		    get { return selectedInputL; }
		    set { selectedInputL = value; }
	    }

	    public int SelectedInputR
	    {
		    get { return selectedInputR; }
		    set { selectedInputR = value; }
	    }

	    public int SelectedOutputL
	    {
		    get { return selectedOutputL; }
		    set { selectedOutputL = value; }
	    }

	    public int SelectedOutputR
	    {
		    get { return selectedOutputR; }
		    set { selectedOutputR = value; }
	    }

	    public PlotModel PlotImpulseLeft
	    {
		    get { return plotImpulseLeft; }
		    set
		    {
			    plotImpulseLeft = value;
				NotifyPropertyChanged();
			    NotifyPropertyChanged(nameof(PlotTop));
			}
	    }

	    public PlotModel PlotImpulseRight
	    {
		    get { return plotImpulseRight; }
		    set
		    {
			    plotImpulseRight = value;
				NotifyPropertyChanged();
			    NotifyPropertyChanged(nameof(PlotBottom));
			}
	    }

	    public Brush ClipLBrush
	    {
		    get { return clipLBrush; }
		    set { clipLBrush = value; NotifyPropertyChanged(); }
	    }

	    public Brush ClipRBrush
	    {
		    get { return clipRBrush; }
		    set { clipRBrush = value; NotifyPropertyChanged(); }
	    }

	    public TabItem SelectedTab
	    {
		    get { return selectedTab; }
		    set
		    {
			    selectedTab = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(PlotTop));
			    NotifyPropertyChanged(nameof(PlotBottom));
			}
	    }

	    public PlotModel PlotTop => selectedTab?.Header?.ToString() == "Master" ? PlotImpulseLeft : SelectedImpulse?.Plot1;
	    public PlotModel PlotBottom => selectedTab?.Header?.ToString() == "Master" ? PlotImpulseRight : SelectedImpulse?.Plot2;
		
	    private void ShowAbout()
	    {
		    var w = new AboutWindow();
		    w.Owner = Application.Current.MainWindow;
		    w.ShowDialog();
	    }
		
		private void ExportWav()
	    {
			var saveFileDialog = new SaveFileDialog();
		    saveFileDialog.Filter = "Wav file (*.wav)|*.wav";
		    saveFileDialog.RestoreDirectory = true;
		    saveFileDialog.InitialDirectory = saveSampleDirectory;

			if (saveFileDialog.ShowDialog() == true)
		    {
			    var doubleIr = new[] {ir[0].Select(x => (double)x).ToArray(), ir[1].Select(x => (double)x).ToArray()};
			    var fileWithoutExt = saveFileDialog.FileName.Substring(0, saveFileDialog.FileName.Length - 4);
			    saveSampleDirectory = Path.GetDirectoryName(saveFileDialog.FileName);

				WaveFiles.WriteWaveFile(doubleIr, WaveFiles.WaveFormat.PCM24Bit, preset.Samplerate, saveFileDialog.FileName);
			    WaveFiles.WriteWaveFile(new[] {doubleIr[0]}, WaveFiles.WaveFormat.PCM24Bit, preset.Samplerate, fileWithoutExt + "-L.wav");
			    WaveFiles.WriteWaveFile(new[] {doubleIr[1]}, WaveFiles.WaveFormat.PCM24Bit, preset.Samplerate, fileWithoutExt + "-R.wav");
		    }
	    }

		private void AudioSetup()
	    {
		    try
		    {
				StopAudio();

				// Use the graphical editor to create a new config
				var config = RealtimeHostConfig.CreateConfig(host.Config);
				LoadAudioConfig(config);
		    }
		    catch (Exception)
		    {
				var config = RealtimeHostConfig.CreateConfig();
			    LoadAudioConfig(config);
		    }
		}

	    private void LoadAudioConfig(RealtimeHostConfig config)
	    {
		    try
		    {
			    if (config != null)
			    {
				    host.SetConfig(config);
				    GetChannelNames(config);
			    }

			    if (host.Config != null)
			    {
				    StartAudio();
				    StopAudio();
				    StartAudio();
			    }
		    }
		    catch (Exception ex)
		    {
			    Logging.ShowMessage("Unable to start audio engine: " + ex.Message, LogType.Error);
		    }

		    NotifyPropertyChanged(nameof(SamplerateWarning));
		}

	    private void GetChannelNames(RealtimeHostConfig config)
	    {
			var inputDeviceInfo = PortAudio.Pa_GetDeviceInfo(config.InputDeviceID);
		    var outputDeviceInfo = PortAudio.Pa_GetDeviceInfo(config.OutputDeviceID);

			InputNames = Enumerable.Range(0, inputDeviceInfo.maxInputChannels)
			    .Select(ch =>
			    {
				    string chName = null;
				    PortAudio.PaAsio_GetInputChannelName((PortAudio.PaDeviceIndex)config.InputDeviceID, ch, ref chName);
				    return (ch+1) + ": " + chName;
			    })
			    .ToArray();

		    OutputNames = Enumerable.Range(0, outputDeviceInfo.maxOutputChannels)
			    .Select(ch =>
			    {
				    string chName = null;
				    PortAudio.PaAsio_GetOutputChannelName((PortAudio.PaDeviceIndex)config.OutputDeviceID, ch, ref chName);
				    return (ch + 1) + ": " + chName;
			    })
			    .ToArray();
		}

	    private void StopAudio()
	    {
			if (host.StreamState == StreamState.Started)
				host.StopStream();
			if (host.StreamState == StreamState.Stopped)
				host.CloseStream();
		}

	    private void StartAudio()
	    {
			if (host.StreamState == StreamState.Closed)
				host.OpenStream();
			if (host.StreamState == StreamState.Open)
				host.StartStream();
		}

		private void UpdateClipIndicators()
	    {
		    while (true)
		    {
			    if ((DateTime.UtcNow - clipTimeLeft).TotalMilliseconds < 500)
			    {
					if (ClipLBrush == Brushes.Transparent)
					    ClipLBrush = Brushes.Red;
				}
			    else
				    ClipLBrush = Brushes.Transparent;

			    if ((DateTime.UtcNow - clipTimeRight).TotalMilliseconds < 500)
			    {
					if (ClipRBrush == Brushes.Transparent)
						ClipRBrush = Brushes.Red;
			    }
			    else
				    ClipRBrush = Brushes.Transparent;

			    Thread.Sleep(20);
		    }

	    }

		private void UpdateSample()
	    {	
			var processor = new ImpulsePresetProcessor(preset);
			var output = processor.Process();
			ir = new[] {output[0].Select(x => (float)x).ToArray(), output[1].Select(x => (float)x).ToArray()};
			var millis = Utils.Linspace(0, preset.ImpulseLength / (double)preset.Samplerate * 1000, preset.ImpulseLength).ToArray();
			/*
			ir = new [] { new float[ir[0].Length], new float[ir[0].Length] };
		    ir[0][0] = 1.0f;
		    ir[1][0] = 1.0f;
			*/
			// Left
			var pm = new PlotModel();

			var line = pm.AddLine(millis, millis.Select(x => 0.0));
			line.StrokeThickness = 1.0;
			line.Color = OxyColor.FromArgb(50, 0, 0, 0);

			line = pm.AddLine(millis, ir[0].Select(x => (double)x));
			line.StrokeThickness = 1.0;
			line.Color = OxyColors.Black;

			PlotImpulseLeft = pm;

			// Right
			pm = new PlotModel();

			line = pm.AddLine(millis, millis.Select(x => 0.0));
			line.StrokeThickness = 1.0;
			line.Color = OxyColor.FromArgb(50, 0, 0, 0);

			line = pm.AddLine(millis, ir[1].Select(x => (double)x));
			line.StrokeThickness = 1.0;
			line.Color = OxyColors.Black;

			PlotImpulseRight = pm;
	    }

	    private void LoadSettings()
	    {
		    try
		    {

			    if (File.Exists(settingsFile))
			    {
				    var jsonString = File.ReadAllText(settingsFile);
				    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

				    if (dict.ContainsKey("AudioSettings"))
				    {
					    var config = RealtimeHostConfig.Deserialize(dict["AudioSettings"]);
					    LoadAudioConfig(config);
				    }

				    if (dict.ContainsKey(nameof(SelectedInputL)))
						SelectedInputL = int.Parse(dict[nameof(SelectedInputL)]);

				    if (dict.ContainsKey(nameof(SelectedInputR)))
						SelectedInputR = int.Parse(dict[nameof(SelectedInputR)]);

				    if (dict.ContainsKey(nameof(SelectedOutputL)))
						SelectedOutputL = int.Parse(dict[nameof(SelectedOutputL)]);

				    if (dict.ContainsKey(nameof(SelectedOutputR)))
						SelectedOutputR = int.Parse(dict[nameof(SelectedOutputR)]);

				    if (dict.ContainsKey(nameof(loadSampleDirectory)))
						loadSampleDirectory = dict[nameof(loadSampleDirectory)];

				    if (dict.ContainsKey(nameof(saveSampleDirectory)))
						saveSampleDirectory = dict[nameof(saveSampleDirectory)];

				    if (dict.ContainsKey(nameof(VolumeSlider)))
					    VolumeSlider = double.Parse(dict[nameof(VolumeSlider)], CultureInfo.InvariantCulture);
			    }

		    }
		    catch (Exception)
		    {
			    Logging.ShowMessage("Failed to load user settings, resetting to default", LogType.Warning);
				File.Delete(settingsFile);
		    }
	    }

		private void SaveSettings()
	    {
		    string currentJson = null;
		    Action create = () =>
		    {
			    var dict = new Dictionary<string, string>();
			    dict["AudioSettings"] = host.Config != null ? host.Config.Serialize() : "";
			    dict[nameof(SelectedInputL)] = SelectedInputL.ToString();
			    dict[nameof(SelectedInputR)] = SelectedInputR.ToString();
			    dict[nameof(SelectedOutputL)] = SelectedOutputL.ToString();
			    dict[nameof(SelectedOutputR)] = SelectedOutputR.ToString();
			    dict[nameof(loadSampleDirectory)] = loadSampleDirectory;
			    dict[nameof(saveSampleDirectory)] = saveSampleDirectory;
			    dict[nameof(VolumeSlider)] = VolumeSlider.ToString("0.000", CultureInfo.InvariantCulture);
				var jsonString = JsonConvert.SerializeObject(dict, Formatting.Indented);
			    if (jsonString != currentJson)
			    {
					if (currentJson != null) // don't save on the first round, only compute the value you indend to save. This is so we don't touch the config every time the app opens
						File.WriteAllText(settingsFile, jsonString);

					currentJson = jsonString;
					
			    }
		    };


			while (true)
			{
				create();
				Thread.Sleep(1000);
			}
	    }

		// ---------------------------------------------- Audio Processing ----------------------------------------

		private float[][] ir;
	    private int bufferIndexL, bufferIndexR;
		private float[] bufferL = new float[65536 * 2];
	    private float[] bufferR = new float[65536 * 2];

	    private unsafe void ProcessAudioDirect(float** inputs, float** outputs, int bufferSize)
	    {
		    if (ir == null)
			    return;

		    var irLeft = ir[0];
		    var irRight = ir[1];
		    if (irLeft == null || irRight == null || irLeft.Length != irRight.Length)
			    return;

		    var selInL = selectedInputL;
		    var selInR = selectedInputR;
		    var selOutL = selectedOutputL;
		    var selOutR = selectedOutputR;
		    var gain = (float)Utils.DB2gain(VolumeDb);

		    //if (selInL >= 0 && selInL < inputs.Length)
		    //{
		    //if (selOutL >= 0 && selOutL < outputs.Length)
		    //{
		    bool clipped = false;
		    //ProcessAudioChannel(inputs[selInL], outputs[selOutL], gain, ref bufferIndexL, irLeft, bufferL, ref clipped);
		    Console.WriteLine($"{DateTime.UtcNow:mm:ss.fff} = Pop");
		    ProcessConv.ProcessUnsafe(inputs[selInL], outputs[selOutL], bufferSize, gain, ref bufferIndexL, irLeft, bufferL, ref clipped);

		    if (clipped)
			    clipTimeLeft = DateTime.UtcNow;

		    //}
		    //}

		    //if (selInR >= 0 && selInR < inputs.Length)
		    //{
		    //if (selOutR >= 0 && selOutR < outputs.Length)
		    //{
		    bool clipped2 = false;
		    //ProcessAudioChannel(inputs[selInR], outputs[selOutR], gain, ref bufferIndexR, irRight, bufferR, ref clipped);
		    ProcessConv.ProcessUnsafe(inputs[selInR], outputs[selOutR], bufferSize, gain, ref bufferIndexR, irRight, bufferR, ref clipped2);
		    if (clipped2)
			    clipTimeRight = DateTime.UtcNow;
		    //}
		    //}*/
	    }

		private void ProcessAudio(float[][] inputs, float[][] outputs)
		{
			if (ir == null)
				return;

			var irLeft = ir[0];
			var irRight = ir[1];
			if (irLeft == null || irRight == null || irLeft.Length != irRight.Length)
				return;

			var selInL = selectedInputL;
			var selInR = selectedInputR;
			var selOutL = selectedOutputL;
			var selOutR = selectedOutputR;
			var gain = (float)Utils.DB2gain(VolumeDb);

			if (selInL >= 0 && selInL < inputs.Length)
			{
				if (selOutL >= 0 && selOutL < outputs.Length)
				{
					bool clipped = false;
					//ProcessAudioChannel(inputs[selInL], outputs[selOutL], ref idxL, ir[0], bufferL, ref clipped);
					ProcessConv.Process(inputs[selInL], outputs[selOutL], gain, ref bufferIndexL, irLeft, bufferL, ref clipped);

					if (clipped)
						clipTimeLeft = DateTime.UtcNow;

				}
			}

			if (selInR >= 0 && selInR < inputs.Length)
			{
				if (selOutR >= 0 && selOutR < outputs.Length)
				{
					bool clipped = false;
					//ProcessAudioChannel(inputs[selInR], outputs[selOutR], ref idxR, ir[1], bufferR, ref clipped);
					ProcessConv.Process(inputs[selInR], outputs[selOutR], gain, ref bufferIndexR, irRight, bufferR, ref clipped);
					if (clipped)
						clipTimeRight = DateTime.UtcNow;
				}
			}
		}

	    private void ProcessAudioChannel(float[] input, float[] output, float gain, ref int idx, float[] ir, float[] buffer, ref bool clipped)
	    {
			var size = buffer.Length - 1;
			var ix = idx;

			for (int i = 0; i < output.Length; i++)
			{
				 var readPos = (ix + i) & size;
				var sample = input[i];
				
				for (int j = 0; j < ir.Length; j++)
				{
					var writePos = (readPos + j) & size;
					buffer[writePos] += sample * ir[j];
				}
				
				var outputSample = buffer[readPos] * gain;
				buffer[readPos] = 0.0f;
				if (outputSample < -0.98f)
				{
					outputSample = -0.98f;
					clipped = true;
				}
				if (outputSample > 0.98f)
				{
					outputSample = 0.98f;
					clipped = true;
				}
				output[i] = outputSample;
			}

			idx += output.Length;
			if (idx > buffer.Length)
				idx -= buffer.Length;
	    }
    }
}

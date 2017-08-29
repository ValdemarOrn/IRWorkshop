using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AudioLib;
using AudioLib.PortAudioInterop;
using ImpulseHd.Serializer;
using LowProfile.Core.Ui;
using LowProfile.Fourier.Double;
using LowProfile.Visuals;
using Microsoft.Win32;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;

namespace ImpulseHd.Ui
{
    class MainViewModel : ViewModelBase
    {
	    private readonly string settingsFile;
		private readonly LastRetainRateLimiter updateRateLimiter;

		// These must be kept or the memory map gets disposed!
	    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
	    private readonly MemoryMappedFile memoryMap;
		private readonly MemoryMappedViewAccessor mmAccessor;
	    private readonly RealtimeProcessManager realtimeProcess;

		private ImpulsePreset preset;
		private string[] inputNames;
	    private string[] outputNames;

	    private bool switchGraphs;
	    private float[][] outputIr;
	    private Complex[] fftLeft;
	    private Complex[] fftRight;
		private string currentJsonSettings;
	    private int selectedInputL;
	    private int selectedInputR;
	    private int selectedOutputL;
	    private int selectedOutputR;
	    private string loadSampleDirectory;
	    private string saveSampleDirectory;
	    private string savePresetDirectory;
	    private double volumeSlider;
		private PlotModel plotImpulseOutputTop;
		private PlotModel plotImpulseOutputBottom;
		private Brush clipLBrush;
	    private Brush clipRBrush;
	    private TabItem selectedTab;
	    private int selectedImpulseConfigIndex;
	    private int stateIndex;

	    private RealtimeHostConfig realtimeConfig;

		public MainViewModel()
		{
			Logging.SetupLogging();
			PortAudio.Pa_Initialize();

			var realtimeProcesingExePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ImpulseHd.RealtimeProcessing.exe");
			realtimeProcess = new RealtimeProcessManager(realtimeProcesingExePath);
			realtimeProcess.PrematureTerminationCallback = logOutput => ExceptionDialog.ShowDialog("Audio engine has died", string.Join("\r\n", logOutput));

			//ensure copy dependency
			// ReSharper disable once UnusedVariable
			var ttt = typeof(RealtimeProcessing.Program);

			var mSec = new MemoryMappedFileSecurity();
			mSec.AddAccessRule(new AccessRule<MemoryMappedFileRights>(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MemoryMappedFileRights.FullControl, AccessControlType.Allow));
			try
			{
				memoryMap = MemoryMappedFile.CreateNew("Global\\CabIRMap", 65536, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, mSec, HandleInheritability.Inheritable);
				mmAccessor = memoryMap.CreateViewAccessor();
			}
			catch (IOException ex)
			{
				if (ex.Message.Contains("already exists"))
				{
					Logging.ShowMessage("CabIR Studio is already running", LogType.Information, true);
					Environment.Exit(0);
				}

				throw;
			}

			preset = new ImpulsePreset();
			ImpulseConfig = new ObservableCollection<ImpulseConfigViewModel>();
			MixingConfig = new MixingViewModel(preset.MixingConfig, preset.SamplerateTransformed) { OnUpdateCallback = () => updateRateLimiter.Pulse() };

			Title = "CabIR Studio - v" + Assembly.GetExecutingAssembly().GetName().Version;
			settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "settings.json");
			
			NewPresetCommand = new DelegateCommand(_ => NewPreset());
			OpenPresetCommand = new DelegateCommand(_ => OpenPreset());
			SavePresetCommand = new DelegateCommand(_ => SavePreset());

			AudioSetupCommand = new DelegateCommand(_ => AudioSetup());
			RestartAudioEngineCommand = new DelegateCommand(_ => RestartAudioEngine());
			ExportWavCommand = new DelegateCommand(_ => ExportWav());
			ShowAboutCommand = new DelegateCommand(_ => ShowAbout());
			CheckForUpdatesCommand = new DelegateCommand(_ => Process.Start("https://github.com/ValdemarOrn/ImpulseEngine"));

			AddImpulseCommand = new DelegateCommand(_ => AddImpulse());
			RemoveImpulseCommand = new DelegateCommand(_ => RemoveImpulse());
			MoveImpulseLeftCommand = new DelegateCommand(_ => MoveImpulseLeft());
			MoveImpulseRightCommand = new DelegateCommand(_ => MoveImpulseRight());
			CloneImpulseCommand = new DelegateCommand(_ => CloneImpulse());
			SwitchGraphsCommand = new DelegateCommand(_ => SwitchGraphs());
			selectedInputL = -1;
		    selectedInputR = -1;
		    selectedOutputL = -1;
		    selectedOutputR = -1;

			updateRateLimiter = new LastRetainRateLimiter(100, Update);

			LoadSettings();
			
			var t = new Thread(SaveSettingsLoop) {IsBackground = true};
			t.Priority = ThreadPriority.Lowest;
			t.Start();

			var t3 = new Thread(UpdateClipIndicators) { IsBackground = true };
			t3.Priority = ThreadPriority.Lowest;
			t3.Start();

			AddImpulse();
			UpdateMemoryMap();
			StartAudioEngine();
		}

	    private void UpdateMemoryMap()
	    {
		    var state = new SharedMemoryState
		    {
			    Gain = (float)Utils.DB2gain(VolumeDb),
			    Id = ++stateIndex,
			    IrLeft = outputIr[0],
			    IrRight = outputIr[1],
			    IrLength = outputIr[0].Length,
			    SelectedInputLeft = SelectedInputL,
			    SelectedInputRight = selectedInputR,
			    SelectedOutputLeft = SelectedOutputL,
			    SelectedOutputRight = SelectedOutputR
		    };

		    state.Write(mmAccessor);
	    }

	    public string Title { get; set; }
		
	    public ObservableCollection<ImpulseConfigViewModel> ImpulseConfig { get; set; }
	    public MixingViewModel MixingConfig { get; set; }

	    public int SelectedImpulseConfigIndex
	    {
		    get { return selectedImpulseConfigIndex; }
		    set
			{
				selectedImpulseConfigIndex = value;
				NotifyPropertyChanged();

				NotifyPropertyChanged(nameof(PlotTop));
				NotifyPropertyChanged(nameof(PlotBottom));
			}
	    }


	    public ImpulseConfigViewModel SelectedImpulse
	    {
		    get
		    {
				var idx = SelectedImpulseConfigIndex;
				var configs = ImpulseConfig;

				if (idx < 0 || idx >= configs.Count)
					return null;

			    return configs[idx];
		    }
	    }

		public ICommand NewPresetCommand { get; }
	    public ICommand OpenPresetCommand { get; }
	    public ICommand SavePresetCommand { get; }
		
		public ICommand RestartAudioEngineCommand { get; }
		public ICommand AudioSetupCommand { get; }
		public ICommand ExportWavCommand { get; }
		public ICommand ShowAboutCommand { get; }
		public ICommand CheckForUpdatesCommand { get; }

	    public ICommand AddImpulseCommand { get; }
	    public ICommand RemoveImpulseCommand { get; }
	    public ICommand MoveImpulseLeftCommand { get; }
	    public ICommand MoveImpulseRightCommand { get; }
		public ICommand CloneImpulseCommand { get; }
		public ICommand SwitchGraphsCommand { get; }

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

	    public double Samplerate
		{
		    get { return preset.Samplerate; }
		    set
			{
				preset.Samplerate = value;

				if (ImpulseConfig != null)
				{
					foreach (var ic in ImpulseConfig)
						ic.Update();
				}

				if (MixingConfig != null)
				{
					MixingConfig.Samplerate = preset.SamplerateTransformed;
				}

				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(SamplerateReadout));
				NotifyPropertyChanged(nameof(SamplerateWarning));
				updateRateLimiter.Pulse();
			}
	    }
		
	    public double ImpulseLength
	    {
		    get { return preset.ImpulseLength; }
		    set
		    {
				preset.ImpulseLength = value;

			    if (ImpulseConfig != null) // triggered on the VM rather than underlying model because this updates the Gui as well
			    {
				    foreach (var ic in ImpulseConfig)
					    ic.ImpulseLength = preset.ImpulseLengthTransformed;
			    }

			    NotifyPropertyChanged();
			    NotifyPropertyChanged(nameof(ImpulseLengthReadout));
			    updateRateLimiter.Pulse();
			}
	    }
	
		public double VolumeSlider
	    {
		    get { return volumeSlider; }
		    set
		    {
			    volumeSlider = value;
			    NotifyPropertyChanged(nameof(VolumeReadout));
				UpdateMemoryMap();
			}
	    }

	    public double VolumeDb => (VolumeSlider * 80 - 60);

	    public string SamplerateReadout => preset.SamplerateTransformed.ToString();
		public string ImpulseLengthReadout => preset.ImpulseLengthTransformed + " - " + string.Format("{0:0.0}ms", preset.ImpulseLengthTransformed / (double)preset.SamplerateTransformed * 1000);
		public string VolumeReadout => $"{VolumeDb:0.0}dB";

	    public string SamplerateWarning => (realtimeProcess.IsRunning && realtimeConfig.Samplerate != preset.SamplerateTransformed)
		    ? "For accurate results, impulse samplerate should match\r\naudio device samplerate"
		    : "";

	    public int SelectedInputL
	    {
		    get { return selectedInputL; }
		    set { selectedInputL = value; UpdateMemoryMap(); }
	    }

	    public int SelectedInputR
	    {
		    get { return selectedInputR; }
		    set { selectedInputR = value; UpdateMemoryMap(); }
	    }

	    public int SelectedOutputL
	    {
		    get { return selectedOutputL; }
		    set { selectedOutputL = value; UpdateMemoryMap(); }
	    }

	    public int SelectedOutputR
	    {
		    get { return selectedOutputR; }
		    set { selectedOutputR = value; UpdateMemoryMap(); }
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

	    public string SelectedTabHeader => selectedTab?.Header?.ToString();

		public PlotModel PlotTop
	    {
		    get
		    {
				if (SelectedTabHeader == "Master")
					return plotImpulseOutputTop;
				if (SelectedTabHeader == "Impulses")
					return SelectedImpulse?.Plot1;
			    if (SelectedTabHeader == "Mixing")
				    return MixingConfig.Plot1;
				return null;
		    }
	    }

	    public PlotModel PlotBottom
	    {
		    get
			{
				if (SelectedTabHeader == "Master")
					return plotImpulseOutputBottom;
				if (SelectedTabHeader == "Impulses")
					return SelectedImpulse?.Plot2;
				if (SelectedTabHeader == "Mixing")
					return plotImpulseOutputBottom;
				return null;
			}
	    }
		
	    private void SwitchGraphs()
	    {
		    switchGraphs = !switchGraphs;
		    UpdateFftPlot();
		    UpdateTimePlot();
		}

		private void NewPreset()
	    {
		    var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		    var defaultPreset = Path.Combine(dir, "Default.cabir");
			var json = File.ReadAllText(defaultPreset);
		    var newPreset = PresetSerializer.DeserializePreset(json);
		    ApplyPreset(newPreset);
		}

	    private void OpenPreset()
	    {
		    var openFileDialog = new OpenFileDialog();
		    openFileDialog.Filter = "CabIR file (*.cabir)|*.cabir";
		    openFileDialog.RestoreDirectory = true;
		    openFileDialog.InitialDirectory = savePresetDirectory;

		    if (openFileDialog.ShowDialog() == true)
		    {
			    savePresetDirectory = Path.GetDirectoryName(openFileDialog.FileName);
			    var json = File.ReadAllText(openFileDialog.FileName);
			    var newPreset = PresetSerializer.DeserializePreset(json);
			    ApplyPreset(newPreset);
		    }
		}

	    private void ApplyPreset(ImpulsePreset newPreset)
	    {
		    preset = newPreset;
		    ImpulseConfig.Clear();
		    foreach (var ic in preset.ImpulseConfig)
		    {
			    var vm = AddImpulseConfigVm(ic);
			    Task.Delay(100).ContinueWith(_ => vm.Update());
		    }

		    SelectedImpulseConfigIndex = 0;
		    NotifyPropertyChanged(nameof(Samplerate));
		    NotifyPropertyChanged(nameof(ImpulseLength));
		    NotifyPropertyChanged(nameof(WindowMethod));
		    NotifyPropertyChanged(nameof(SamplerateReadout));
		    NotifyPropertyChanged(nameof(ImpulseLengthReadout));
			updateRateLimiter.Pulse();
		}
		
	    private void SavePreset()
	    {
		    var json = PresetSerializer.SerializePreset(preset);

		    var saveFileDialog = new SaveFileDialog();
		    saveFileDialog.Filter = "CabIR file (*.cabir)|*.cabir";
		    saveFileDialog.RestoreDirectory = true;
		    saveFileDialog.InitialDirectory = savePresetDirectory;

		    if (saveFileDialog.ShowDialog() == true)
		    {
			    savePresetDirectory = Path.GetDirectoryName(saveFileDialog.FileName);
			    File.WriteAllText(saveFileDialog.FileName, json);
		    }
		}

		private void CloneImpulse()
		{
			var idx = selectedImpulseConfigIndex;
			if (idx < 0 || idx >= preset.ImpulseConfig.Length)
				return;

			var json = PresetSerializer.SerializeImpulse(preset.ImpulseConfig[selectedImpulseConfigIndex]);
			var ic = PresetSerializer.DeserializeImpulse(json);

			preset.ImpulseConfig = preset.ImpulseConfig.Concat(new[] { ic }).ToArray();
			var vm = AddImpulseConfigVm(ic);
			Task.Delay(100).ContinueWith(_ => vm.Update());
			SelectedImpulseConfigIndex = ImpulseConfig.Count - 1;
		}

		private void MoveImpulseRight()
	    {
		    var idx = selectedImpulseConfigIndex;

			if (SelectedImpulse == null)
				return;
			if (idx == ImpulseConfig.Count - 1)
				return;

		    var aVm = ImpulseConfig[idx];
			var bVm = ImpulseConfig[idx + 1];
		    var aIc = preset.ImpulseConfig[idx];
		    var bIc = preset.ImpulseConfig[idx + 1];

			preset.ImpulseConfig[idx] = bIc;
			preset.ImpulseConfig[idx + 1] = aIc;
			ImpulseConfig[idx] = bVm;
			ImpulseConfig[idx + 1] = aVm;

		    SelectedImpulseConfigIndex = idx + 1;
		}

	    private void MoveImpulseLeft()
	    {
		    var idx = selectedImpulseConfigIndex;

			if (SelectedImpulse == null)
			    return;
		    if (idx == 0)
			    return;

		    var aVm = ImpulseConfig[idx];
		    var bVm = ImpulseConfig[idx - 1];
		    var aIc = preset.ImpulseConfig[idx];
		    var bIc = preset.ImpulseConfig[idx - 1];

		    preset.ImpulseConfig[idx] = bIc;
		    preset.ImpulseConfig[idx - 1] = aIc;
		    ImpulseConfig[idx] = bVm;
		    ImpulseConfig[idx - 1] = aVm;

		    SelectedImpulseConfigIndex = idx - 1;
	    }

	    private void RemoveImpulse()
	    {
		    if (SelectedImpulse == null)
			    return;

		    var impulses = new List<ImpulseConfig>();
		    foreach (var ic in preset.ImpulseConfig)
		    {
			    if (ic != SelectedImpulse.ImpulseConfig)
				    impulses.Add(ic);
		    }

			preset.ImpulseConfig = impulses.ToArray();
		    ImpulseConfig.RemoveAt(selectedImpulseConfigIndex);
		    updateRateLimiter.Pulse();
		}

	    private void AddImpulse()
	    {
			var ic = new ImpulseConfig {Name = "New Impulse", Samplerate = preset.SamplerateTransformed };
		    preset.ImpulseConfig = preset.ImpulseConfig.Concat(new[] { ic }).ToArray();
			var vm = AddImpulseConfigVm(ic);
			Task.Delay(100).ContinueWith(_ => vm.Update());
			SelectedImpulseConfigIndex = ImpulseConfig.Count - 1;
		    updateRateLimiter.Pulse();
		}

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
			    var doubleIr = new[] {outputIr[0].Select(x => (double)x).ToArray(), outputIr[1].Select(x => (double)x).ToArray()};
			    var fileWithoutExt = saveFileDialog.FileName.Substring(0, saveFileDialog.FileName.Length - 4);
			    saveSampleDirectory = Path.GetDirectoryName(saveFileDialog.FileName);

				WaveFiles.WriteWaveFile(doubleIr, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, saveFileDialog.FileName);
			    WaveFiles.WriteWaveFile(new[] {doubleIr[0]}, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, fileWithoutExt + "-L.wav");
			    WaveFiles.WriteWaveFile(new[] {doubleIr[1]}, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, fileWithoutExt + "-R.wav");
		    }
	    }

	    private void RestartAudioEngine()
	    {
		    StartAudioEngine();
	    }

		private void AudioSetup()
	    {
			StopAudioEngine();

			try
		    {
				// Use the graphical editor to create a new config
			    var config = RealtimeHostConfig.CreateConfig(realtimeConfig);
				if (config != null)
					realtimeConfig = config;
		    }
		    catch (Exception)
		    {
				realtimeConfig = RealtimeHostConfig.CreateConfig();
		    }

			GetChannelNames(realtimeConfig);
			NotifyPropertyChanged(nameof(SamplerateWarning));
		    SaveSettings();
		    UpdateMemoryMap();
			StartAudioEngine();
	    }
		
	    private ImpulseConfigViewModel AddImpulseConfigVm(ImpulseConfig ic)
	    {
		    var vm = new ImpulseConfigViewModel(ic, loadSampleDirectory)
		    {
			    OnUpdateCallback = () => updateRateLimiter.Pulse(),
			    OnLoadSampleCallback = dir => loadSampleDirectory = Path.GetDirectoryName(dir),
			    ImpulseLength = preset.ImpulseLengthTransformed,
			    Samplerate = preset.SamplerateTransformed
		    };

			ImpulseConfig.Add(vm);
			return vm;
	    }

		private void GetChannelNames(RealtimeHostConfig config)
	    {
		    if (config == null)
		    {
			    InputNames = new string[0];
			    OutputNames = new string[0];
				return;
		    }

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

	    private void StopAudioEngine()
	    {
		    realtimeProcess.StopProcess();
	    }

	    private void StartAudioEngine()
	    {
		    try
		    {
			    realtimeProcess.StartProcess();
		    }
		    catch (Exception ex)
		    {
				Logging.ShowMessage("Unable to start audio engine: " + ex.Message, LogType.Error);
			}
		}

		private void UpdateClipIndicators()
	    {
		    while (true)
		    {
			    try
			    {
				    var clipTimes = SharedMemoryState.ReadClipIndicators(mmAccessor);

				    if ((DateTime.UtcNow - clipTimes[0]).TotalMilliseconds < 500)
				    {
					    if (ClipLBrush.Equals(Brushes.Transparent))
						    ClipLBrush = Brushes.Red;
				    }
				    else
					    ClipLBrush = Brushes.Transparent;

				    if ((DateTime.UtcNow - clipTimes[1]).TotalMilliseconds < 500)
				    {
					    if (ClipRBrush.Equals(Brushes.Transparent))
						    ClipRBrush = Brushes.Red;
				    }
				    else
					    ClipRBrush = Brushes.Transparent;
			    }
			    catch (Exception)
			    {
				    Thread.Sleep(500);
					// swallow
			    }

			    Thread.Sleep(20);
		    }
		    // ReSharper disable once FunctionNeverReturns
	    }

		private void Update()
	    {	
			var processor = new ImpulsePresetProcessor(preset);
			var output = processor.Process();
			outputIr = new[] {output[0].Select(x => (float)x).ToArray(), output[1].Select(x => (float)x).ToArray()};

			// convert final, commplete output back to Fft to show the final frequency response

			var fftTransform = new LowProfile.Fourier.Double.Transform(outputIr.Length);

			var timeDomainLeft = outputIr[0].Select(x => (Complex)x).ToArray();
			fftLeft = new Complex[timeDomainLeft.Length];
		    fftTransform.FFT(timeDomainLeft, fftLeft);

		    var timeDomainRight = outputIr[1].Select(x => (Complex)x).ToArray();
		    fftRight = new Complex[timeDomainRight.Length];
		    fftTransform.FFT(timeDomainRight, fftRight);
			
			UpdateMemoryMap();
		    UpdateFftPlot();
			UpdateTimePlot();
	    }

	    private void UpdateFftPlot()
	    {
		    var magDataLeft = fftLeft.Take(fftLeft.Length / 2).Select(x => x.Abs).Select(x => Utils.Gain2DB(x)).ToArray();
		    var magDataRight = fftRight.Take(fftRight.Length / 2).Select(x => x.Abs).Select(x => Utils.Gain2DB(x)).ToArray();
			var hz = Utils.Linspace(0, 0.5, magDataLeft.Length).Select(x => x * (double)preset.SamplerateTransformed).ToArray();

		    var pm = new PlotModel();

		    pm.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Minimum = 20 });
		    var leftAxis = new LinearAxis { Position = AxisPosition.Left, Key = "LeftAxis", Minimum = -80 };
		    //var rightAxis = new LinearAxis { Position = AxisPosition.Right, Key = "RightAxis", Minimum = -Math.PI - 0.1, Maximum = Math.PI + 0.1 };
		    pm.Axes.Add(leftAxis);
		    //pm.Axes.Add(rightAxis);

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

			line = pm.AddLine(hz, magDataLeft);
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromAColor(127, OxyColors.Blue);
		    line.YAxisKey = "LeftAxis";

		    line = pm.AddLine(hz, magDataRight);
		    line.StrokeThickness = 1.0;
			line.Color = OxyColor.FromAColor(127, OxyColors.Red);
			line.YAxisKey = "LeftAxis";

			if (switchGraphs)
				plotImpulseOutputTop = pm;
			else
				plotImpulseOutputBottom = pm;

			NotifyPropertyChanged(nameof(PlotTop));
		    NotifyPropertyChanged(nameof(PlotBottom));
		}

	    private void UpdateTimePlot()
	    {
			var millis = Utils.Linspace(0, preset.ImpulseLengthTransformed / (double)preset.SamplerateTransformed * 1000, preset.ImpulseLengthTransformed).ToArray();
			
			// left and right
			var pm = new PlotModel();

		    var line = pm.AddLine(millis, millis.Select(x => 0.0));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromArgb(50, 0, 0, 0);

		    line = pm.AddLine(millis, outputIr[0].Select(x => (double)x));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromAColor(127, OxyColors.Blue);

		    line = pm.AddLine(millis, outputIr[1].Select(x => (double)x));
		    line.StrokeThickness = 1.0;
		    line.Color = OxyColor.FromAColor(127, OxyColors.Red);

			if (switchGraphs)
				plotImpulseOutputBottom = pm;
			else
				plotImpulseOutputTop = pm;

			NotifyPropertyChanged(nameof(PlotTop));
		    NotifyPropertyChanged(nameof(PlotBottom));
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
						realtimeConfig = RealtimeHostConfig.Deserialize(dict["AudioSettings"]);
						GetChannelNames(realtimeConfig);
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

				    if (dict.ContainsKey(nameof(savePresetDirectory)))
					    savePresetDirectory = dict[nameof(savePresetDirectory)];
					
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
			var dict = new Dictionary<string, string>();
			dict["AudioSettings"] = realtimeConfig != null ? realtimeConfig.Serialize() : "";
			dict[nameof(SelectedInputL)] = SelectedInputL.ToString();
			dict[nameof(SelectedInputR)] = SelectedInputR.ToString();
			dict[nameof(SelectedOutputL)] = SelectedOutputL.ToString();
			dict[nameof(SelectedOutputR)] = SelectedOutputR.ToString();
			dict[nameof(loadSampleDirectory)] = loadSampleDirectory;
			dict[nameof(saveSampleDirectory)] = saveSampleDirectory;
			dict[nameof(savePresetDirectory)] = savePresetDirectory;
			dict[nameof(VolumeSlider)] = VolumeSlider.ToString("0.000", CultureInfo.InvariantCulture);
			var jsonString = JsonConvert.SerializeObject(dict, Formatting.Indented);
			if (jsonString != currentJsonSettings)
			{
				if (currentJsonSettings != null) // don't save on the first round, only compute the value you indend to save. This is so we don't touch the config every time the app opens
					File.WriteAllText(settingsFile, jsonString);

				currentJsonSettings = jsonString;
			}
		}

		private void SaveSettingsLoop()
	    {
			while (true)
			{
				try
				{
					SaveSettings();
				}
				catch (Exception)
				{
					//swallow
				}
				Thread.Sleep(1000);
			}
		    // ReSharper disable once FunctionNeverReturns
	    }
    }
}

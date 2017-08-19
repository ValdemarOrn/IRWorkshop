using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AudioLib.PortAudioInterop;
using LowProfile.Core.Ui;
using LowProfile.Fourier.Double;

namespace ImpulseHd.Ui
{
    class MainViewModel : ViewModelBase
    {
	    private RealtimeHost host;

		public MainViewModel()
	    {
			
			ImpulseConfig = new ObservableCollection<ImpulseConfigViewModel>();
			ImpulseConfig.Add(new ImpulseConfigViewModel() { Name = "My Impulse 123", FilePath = @"E:\Sound\Samples\Impulses\OwnHammer\_Best Picks 500ms 48Khz\OH 412 ENG 12C+V30 JS.wav" });
			ImpulseConfig[0].LoadSampleData();

		    // The host lives in a singleton withing. It can not be created directly 
		    // and only one host can exists within an application context
		    host = RealtimeHost.Host;

		    // host.Process is the callback method that processes audio data. 
		    // Assign the static process method in this class to be the callback
		    host.Process = ProcessAudio;

		    AudioSetupCommand = new DelegateCommand(_ => AudioSetup());
		}

	    private void AudioSetup()
	    {
		    StopAudio();

			// Use the graphical editor to create a new config
		    var config = RealtimeHostConfig.CreateConfig();
		    if (config != null)
		    {
			    host.SetConfig(config);
			    GetChannelNames(config);
		    }

		    if (host.Config != null)
				StartAudio();
		}

	    private void GetChannelNames(RealtimeHostConfig config)
	    {
			var inputDeviceInfo = PortAudio.Pa_GetDeviceInfo(config.InputDeviceID);
		    var outputDeviceInfo = PortAudio.Pa_GetDeviceInfo(config.OutputDeviceID);

			var inputNames = Enumerable.Range(0, inputDeviceInfo.maxInputChannels)
			    .Select(ch =>
			    {
				    string chName = null;
				    PortAudio.PaAsio_GetInputChannelName((PortAudio.PaDeviceIndex)config.InputDeviceID, ch, ref chName);
				    return chName;
			    })
			    .ToArray();

		    var outputNames = Enumerable.Range(0, outputDeviceInfo.maxOutputChannels)
			    .Select(ch =>
			    {
				    string chName = null;
				    PortAudio.PaAsio_GetOutputChannelName((PortAudio.PaDeviceIndex)config.OutputDeviceID, ch, ref chName);
				    return chName;
			    })
			    .ToArray();
		}

	    private void StopAudio()
	    {
			if (host.StreamState == StreamState.Started)
				host.StopStream();
			if (host.StreamState == StreamState.Open)
				host.CloseStream();
		}

	    private void StartAudio()
	    {
			if (host.StreamState == StreamState.Closed)
				host.OpenStream();
			if (host.StreamState == StreamState.Open)
				host.StartStream();
		}

	    private void ProcessAudio(float[][] arg1, float[][] arg2)
	    {

	    }

	    public ObservableCollection<ImpulseConfigViewModel> ImpulseConfig { get; private set; }

		public ICommand AudioSetupCommand { get; private set; }
	}
}

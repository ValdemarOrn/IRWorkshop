using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioLib;
using AudioLib.PortAudioInterop;
using Newtonsoft.Json;

namespace ImpulseHd.RealtimeProcessing
{
	class Program
	{
		// set by external process

		private int stateIndex;
		private float[][] ir;
		private int selectedInputL;
		private int selectedInputR;
		private int selectedOutputL;
		private int selectedOutputR;
		private float gain;

		// sent back to external process

		private DateTime clipTimeLeft;
		private DateTime clipTimeRight;

		// internal state 

		private RealtimeHost host;
		private int bufferIndexL, bufferIndexR;
		private float[] bufferL = new float[65536 * 2];
		private float[] bufferR = new float[65536 * 2];
		private MemoryMappedFile memoryMap;
		private MemoryMappedViewAccessor mmAccessor;
		
		static void Main(string[] args)
		{
			new Program().Start();
		}
		
		private void Start()
		{
			selectedInputL = 0;
			selectedInputR = 0;
			selectedOutputL = 0;
			selectedOutputR = 1;

			// The host lives in a singleton withing. It can not be created directly 
			// and only one host can exists within an application context
			host = RealtimeHost.Host;

			// host.Process is the callback method that processes audio data. 
			// Assign the static process method in this class to be the callback
			host.Process = ProcessAudio;

			var settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "settings.json");
			var jsonString = File.ReadAllText(settingsFile);
			var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
			var realtimeConfig = RealtimeHostConfig.Deserialize(dict["AudioSettings"]);
			
			try
			{
				this.memoryMap = MemoryMappedFile.OpenExisting("Global\\CabIRMap");
				this.mmAccessor = memoryMap.CreateViewAccessor();
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Failed to open memory map");
				Environment.Exit(1);
			}

			LoadAudioConfig(realtimeConfig);

			while (true)
			{
				Thread.Sleep(100);
			}
		}
		
		private void LoadAudioConfig(RealtimeHostConfig config)
		{
			if (config != null)
			{
				host.SetConfig(config);
			}

			if (host.Config != null)
			{
				StartAudio();
				StopAudio();
				StartAudio();
			}
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

		private void CheckMemoryMap()
		{
			var state = SharedMemoryState.Load(this.mmAccessor, stateIndex);
			if (state != null)
			{
				stateIndex = state.Id;
				ir = new[] { state.IrLeft, state.IrRight };
				selectedInputL = state.SelectedInputLeft;
				selectedInputR = state.SelectedInputRight;
				selectedOutputL = state.SelectedOutputLeft;
				selectedOutputR = state.SelectedOutputRight;
				gain = state.Gain; 
			}
		}

		private void ProcessAudio(float[][] inputs, float[][] outputs)
		{
			CheckMemoryMap();

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

			if (selInL >= 0 && selInL < inputs.Length)
			{
				if (selOutL >= 0 && selOutL < outputs.Length)
				{
					bool clipped = false;
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
					ProcessConv.Process(inputs[selInR], outputs[selOutR], gain, ref bufferIndexR, irRight, bufferR, ref clipped);
					if (clipped)
						clipTimeRight = DateTime.UtcNow;
				}
			}
		}
	}
}

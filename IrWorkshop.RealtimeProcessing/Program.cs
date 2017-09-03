using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Threading;
using AudioLib.PortAudioInterop;
using Newtonsoft.Json;

namespace IrWorkshop.RealtimeProcessing
{
	public class Program
	{
		// set by external process

		private int stateIndex;
		private float[][] impulseResponse;
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
			//System.Diagnostics.Debugger.Launch();
			new Program().Start();
		}
		
		private void Start()
		{
			selectedInputL = 0;
			selectedInputR = 0;
			selectedOutputL = 0;
			selectedOutputR = 1;

			// initialize PortAudio and get singleton host object
			host = RealtimeHost.Host;
			host.Process = ProcessAudio;

			var settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "settings.json");
			var jsonString = File.ReadAllText(settingsFile);
			var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
			var realtimeConfig = RealtimeHostConfig.Deserialize(dict["AudioSettings"]);
			
			try
			{
				memoryMap = MemoryMappedFile.OpenExisting("Global\\IRWorkshopMap");
				mmAccessor = memoryMap.CreateViewAccessor();
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Failed to open memory map");
				Environment.Exit(1);


				// for testing only
				/*
				this.memoryMap = MemoryMappedFile.CreateNew("Global\\IRWorkshopMap", 65536);
				this.mmAccessor = memoryMap.CreateViewAccessor();
				var state = new SharedMemoryState { Gain = 1.0f, Id = 1, IrLeft = new[] { 1.0f, 0.0f }, IrRight = new[] { 1.0f, 0.0f }, IrLength = 2, SelectedInputLeft = 0, SelectedInputRight = 0, SelectedOutputLeft = 0, SelectedOutputRight = 0 };
				state.Write(mmAccessor);
				*/
			}

			LoadAudioConfig(realtimeConfig);

			while (true)
			{
				Thread.Sleep(1000);
			}
		}
		
		private void LoadAudioConfig(RealtimeHostConfig config)
		{
			Console.WriteLine("Loading RealtimeHostConfig with the following settings:");
			Console.WriteLine(config.Serialize());

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

		private void ReadMemoryMap()
		{
			var state = SharedMemoryState.Read(mmAccessor, stateIndex);
			if (state != null)
			{
				Console.WriteLine("Status update, StateId: {0}, Left IR Length: {1} Right IR Length: {2}", state.Id, state.IrLeft.Length, state.IrRight.Length);
				stateIndex = state.Id;
				impulseResponse = new[] { state.IrLeft, state.IrRight };
				selectedInputL = state.SelectedInputLeft;
				selectedInputR = state.SelectedInputRight;
				selectedOutputL = state.SelectedOutputLeft;
				selectedOutputR = state.SelectedOutputRight;
				gain = state.Gain; 
			}
		}

		private void ProcessAudio(float[][] inputs, float[][] outputs)
		{
			ReadMemoryMap();

			if (impulseResponse == null)
				return;

			var irLeft = impulseResponse[0];
			var irRight = impulseResponse[1];
			if (irLeft == null || irRight == null || irLeft.Length != irRight.Length)
				return;

			var selInL = selectedInputL;
			var selInR = selectedInputR;
			var selOutL = selectedOutputL;
			var selOutR = selectedOutputR;
			bool clipAny = false;

			if (selInL >= 0 && selInL < inputs.Length)
			{
				if (selOutL >= 0 && selOutL < outputs.Length)
				{
					bool clipped = false;
					ProcessConv.Process(inputs[selInL], outputs[selOutL], gain, ref bufferIndexL, irLeft, bufferL, ref clipped);
					clipAny = clipped;

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
					clipAny = clipAny | clipped;

					if (clipped)
						clipTimeRight = DateTime.UtcNow;
				}
			}

			if(clipAny)
				SharedMemoryState.WriteClipIndicators(mmAccessor, clipTimeLeft, clipTimeRight);
		}
	}
}

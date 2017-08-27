using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LowProfile.Core;

namespace ImpulseHd
{
	public class RealtimeProcessManager
	{
		private readonly string exePath;
		private Process process;
		private bool shouldBeAlive;

		private List<string> logOutput;

		public RealtimeProcessManager(string exePath)
		{
			this.exePath = exePath;
			var t = new Thread(Watcher);
			t.IsBackground = true;
			t.Priority = ThreadPriority.Lowest;
			t.Start();
		}

		public Action<IList<string>> PrematureTerminationCallback { get; set; }

		private void Watcher()
		{
			while (true)
			{
				if (shouldBeAlive && (process == null || process.HasExited))
				{
					PrematureTerminationCallback?.Invoke(logOutput.ToArray());
					StopProcess();
				}

				Thread.Sleep(100);
			}
		}

		public bool IsRunning => process != null && !process.HasExited;

		public void StartProcess()
		{
			StopProcess();

			logOutput = new List<string>();
			this.process = new Process();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = exePath,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden,
			};

			process.Start();

			Task.Run(() =>
			{
				while (true)
				{
					var line = process.StandardOutput.ReadLine();
					if (line == null)
						break;

					lock (logOutput)
					{
						logOutput.Add(line);
					}
				}
			});

			Task.Run(() =>
			{
				while (true)
				{
					var line = process.StandardError.ReadLine();
					if (line == null)
						break;

					lock (logOutput)
					{
						logOutput.Add(line);
					}
				}
			});
			
			
			ChildProcessTracker.AddProcess(process);
			shouldBeAlive = true;
		}

		public void StopProcess()
		{
			shouldBeAlive = false;
			if (process != null && !process.HasExited)
			{
				process.Kill();
				process.WaitForExit();
				process = null;
			}
		}
	}
}

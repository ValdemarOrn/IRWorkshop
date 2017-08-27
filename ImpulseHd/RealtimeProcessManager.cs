using System;
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

		public RealtimeProcessManager(string exePath)
		{
			this.exePath = exePath;
			var t = new Thread(Watcher);
			t.IsBackground = true;
			t.Priority = ThreadPriority.Lowest;
			t.Start();
		}

		public Action PrematureTerminationCallback { get; set; }

		private void Watcher()
		{
			while (true)
			{
				if (shouldBeAlive && (process == null || process.HasExited))
				{
					PrematureTerminationCallback?.Invoke();
					StopProcess();
				}

				Thread.Sleep(100);
			}
		}

		public bool IsRunning => process != null && !process.HasExited;

		public void StartProcess()
		{
			StopProcess();

			this.process = new Process();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = exePath,

			};

			process.Start();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LowProfile.Core.Extensions;

namespace IrWorkshop.Ui
{
	public class LastRetainRateLimiter
	{
		private readonly int refreshMillis;
		private readonly Action action;
		private readonly AutoResetEvent updateEvent;
		private Thread thread;

		private volatile bool running;

		public LastRetainRateLimiter(int refreshMillis, Action action)
		{
			this.refreshMillis = refreshMillis;
			this.action = action;
			running = true;

			updateEvent = new AutoResetEvent(true);
			this.thread = new Thread(UpdateLoop);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.Lowest;
			thread.Start();
		}

		public void Pulse()
		{
			updateEvent.Set();
		}

		public void Stop()
		{
			running = false;
			thread.Join();
		}

		private void UpdateLoop()
		{
			while (running)
			{
				try
				{
					var isSet = updateEvent.WaitOne(refreshMillis);
					if (!isSet)
						continue;

					action();
				}
				catch (Exception ex)
				{
					Logging.Exception(ex.Message, ex.GetTrace());
					Thread.Sleep(1000);
				}

				Thread.Sleep(refreshMillis);
			}
		}
	}
}

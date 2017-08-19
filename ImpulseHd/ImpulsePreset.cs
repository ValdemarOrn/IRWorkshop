using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class ImpulsePreset
	{
		public int Samplerate { get; set; }
		public int ImpulseLength { get; set; }

		public WindowMethod WindowMethod { get; set; }
		public double WindowLength { get; set; }

		public ImpulseConfig[] ImpulseConfig { get; set; }

		public ImpulsePreset()
		{
			Samplerate = 48000;
			ImpulseLength = 1024;
			WindowLength = 0.01;
			ImpulseConfig = new ImpulseConfig[0];
		}
	}
}

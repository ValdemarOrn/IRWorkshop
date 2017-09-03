using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using Newtonsoft.Json;

namespace IrWorkshop
{
	public class ImpulsePreset
	{
		[JsonIgnore]
		private double samplerate;

		public ImpulsePreset()
		{
			PresetVersion = 1000;
			Samplerate = 0.3333333;
			ImpulseLength = 0.75;
			ImpulseConfig = new ImpulseConfig[0];
			MixingConfig = new MixingConfig();
		}

		public ImpulseConfig[] ImpulseConfig { get; set; }
		public MixingConfig MixingConfig { get; set; }

		public int PresetVersion { get; set; }

		public double Samplerate
		{
			get { return samplerate; }
			set
			{
				samplerate = value;
				if (ImpulseConfig == null)
					return;

				foreach (var ic in ImpulseConfig)
				{
					ic.Samplerate = SamplerateTransformed;
				}
			}
		}

		public double ImpulseLength { get; set; }
		
		public int SamplerateTransformed
		{
			get
			{
				if (Samplerate < 0.25)
					return 44100;
				else if (Samplerate < 0.5)
					return 48000;
				else if (Samplerate < 0.75)
					return 44100 * 2;
				else
					return 96000;
			}
			set
			{
				if (value == 44100)
					Samplerate = 0.0;
				else if (value == 48000)
					Samplerate = 0.3;
				else if (value == 88200)
					Samplerate = 0.6;
				else if (value == 96000)
					Samplerate = 0.99;
				else
					throw new ArgumentException($"Unsupported Samplerate of {value}");
			}
		}

		public int ImpulseLengthTransformed
		{
			get
			{
				var iVal = (int)((ImpulseLength - 0.0001) * 5);
				if (iVal == 0)
					return 256;
				else if (iVal == 1)
					return 512;
				else if (iVal == 2)
					return 1024;
				else if (iVal == 3)
					return 2048;
				else
					return 4096;
			}
			set
			{
				if (value == 256)
					ImpulseLength = 0.0;
				else if (value == 512)
					ImpulseLength = 0.21;
				else if (value == 1024)
					ImpulseLength = 0.41;
				else if (value == 2048)
					ImpulseLength = 0.65;
				else if (value == 4096)
					ImpulseLength = 0.81;
				else
					throw new ArgumentException($"Unsupported Impulse length of {value}");
			}
		}
	}
}

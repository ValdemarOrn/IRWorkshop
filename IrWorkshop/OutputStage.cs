using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;

namespace ImpulseHd
{
	public class OutputStage
	{
		public OutputStage()
		{
			Gain = 6 / 8.0;
			Pan = 0.5;
			HighCutLeft = 1.0;
			HighCutRight = 1.0;
			WindowMethod = 0.7;
			WindowLength = 0.0;
			LowCut12dB = false;
			HighCut12dB = true;
		}

		public double Gain { get; set; }
		public double DelayMillisL { get; set; }
		public double DelayMillisR { get; set; } // additional delay, <0 := left channel delayed more, >0 right channel delayed more
		public double Pan { get; set; }
		public bool InvertPhaseLeft { get; set; }
		public bool InvertPhaseRight { get; set; }
		public bool LowCut12dB { get; set; }
		public bool HighCut12dB { get; set; }
		public double LowCutLeft { get; set; }
		public double LowCutRight { get; set; }
		public double HighCutLeft { get; set; }
		public double HighCutRight { get; set; }

		public double WindowMethod { get; set; }
		public double WindowLength { get; set; }


		public double GainTransformed => -60 + Gain * 80;
		public double DelayMillisLTransformed => ValueTables.Get(DelayMillisL, ValueTables.Response2Oct) * 80;
		public double DelayMillisRTransformed => ValueTables.Get(DelayMillisR, ValueTables.Response2Oct) * 80;
		public double PanTransformed => Pan * 2 - 1;
		public double LowCutLeftTransformed => 20 + ValueTables.Get(LowCutLeft, ValueTables.Response3Oct) * 1480;
		public double LowCutRightTransformed => 20 + ValueTables.Get(LowCutRight, ValueTables.Response3Oct) * 1480;
		public double HighCutLeftTransformed => 1000 + ValueTables.Get(HighCutLeft, ValueTables.Response4Oct) * 21000;
		public double HighCutRightTransformed => 1000 + ValueTables.Get(HighCutRight, ValueTables.Response4Oct) * 21000;

		public WindowMethod WindowMethodTransformed
		{
			get
			{
				if (WindowMethod < 0.25)
					return ImpulseHd.WindowMethod.Truncate;
				if (WindowMethod < 0.5)
					return ImpulseHd.WindowMethod.Linear;
				if (WindowMethod < 0.75)
					return ImpulseHd.WindowMethod.Logarithmic;
				else
					return ImpulseHd.WindowMethod.Cosine;
			}
		}
		public double WindowLengthTransformed => ValueTables.Get(WindowLength, ValueTables.Response2Oct) * 0.5;
	}
}

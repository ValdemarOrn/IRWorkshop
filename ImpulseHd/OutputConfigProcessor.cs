using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using AudioLib.Modules;

namespace ImpulseHd
{
	public class OutputConfigProcessor
	{
		private readonly double[][] timeSignal;
		private readonly OutputStage outputStage;
		private readonly int impulseLength;
		private readonly double samplerate;

		public OutputConfigProcessor(double[][] timeSignal, OutputStage outputStage, int impulseLength, int samplerate)
		{
			this.timeSignal = timeSignal;
			this.outputStage = outputStage;
			this.impulseLength = impulseLength;
			this.samplerate = samplerate;
		}

		public double[][] ProcessOutputStage()
		{
			var len = timeSignal[0].Length;
			var stereoSignal = timeSignal;

			var gain = Utils.DB2gain(outputStage.GainTransformed);
			var pan = outputStage.PanTransformed;
			var leftPan = pan <= 0 ? 1.0 : 1 - Math.Abs(pan);
			var rightPan = pan >= 0 ? 1.0 : 1 - Math.Abs(pan);
			var leftInvert = outputStage.InvertPhaseLeft ? -1 : 1;
			var rightInvert = outputStage.InvertPhaseRight ? -1 : 1;
			var leftDelay = outputStage.SampleDelayLTransformed;
			var rightDelay = outputStage.SampleDelayRTransformed;
			var windowLen = outputStage.WindowLengthTransformed;
			var windowType = outputStage.WindowMethodTransformed;
			var low12db = outputStage.LowCut12dB;
			var high12db = outputStage.HighCut12dB;

			var lp1Left = new ZeroLp1();
			var lp1Right = new ZeroLp1();
			var hp1Left = new ZeroHp1();
			var hp1Right = new ZeroHp1();

			lp1Left.SetFc(outputStage.HighCutLeftTransformed / (samplerate / 2));
			lp1Right.SetFc(outputStage.HighCutRightTransformed / (samplerate / 2));
			hp1Left.SetFc(outputStage.LowCutLeftTransformed / (samplerate / 2));
			hp1Right.SetFc(outputStage.LowCutRightTransformed / (samplerate / 2));

			var lp2Left = new Biquad { Q = 0.707, Type = Biquad.FilterType.LowPass, Gain = 1, Samplerate = samplerate };
			var lp2Right = new Biquad { Q = 0.707, Type = Biquad.FilterType.LowPass, Gain = 1, Samplerate = samplerate };
			var hp2Left = new Biquad { Q = 0.707, Type = Biquad.FilterType.HighPass, Gain = 1, Samplerate = samplerate };
			var hp2Right = new Biquad { Q = 0.707, Type = Biquad.FilterType.HighPass, Gain = 1, Samplerate = samplerate };

			lp2Left.Frequency = outputStage.HighCutLeftTransformed;
			lp2Right.Frequency = outputStage.HighCutRightTransformed;
			hp2Left.Frequency = outputStage.LowCutLeftTransformed;
			hp2Right.Frequency = outputStage.LowCutRightTransformed;

			lp2Left.Update();
			lp2Right.Update();
			hp2Left.Update();
			hp2Right.Update();

			var outputLeft = new double[len];
			var outputRight = new double[len];

			for (int i = 0; i < stereoSignal[0].Length; i++)
			{
				var lSample = stereoSignal[0][i] * gain * leftPan * leftInvert;
				var rSample = stereoSignal[1][i] * gain * rightPan * rightInvert;

				if (high12db)
				{
					lSample = lp2Left.Process(lSample);
					rSample = lp2Right.Process(rSample);
				}
				else
				{
					lSample = lp1Left.Process(lSample);
					rSample = lp1Right.Process(rSample);
				}

				if (low12db)
				{
					lSample = hp2Left.Process(lSample);
					rSample = hp2Right.Process(rSample);
				}
				else
				{
					lSample = hp1Left.Process(lSample);
					rSample = hp1Right.Process(rSample);
				}

				if (i + leftDelay < len)
					outputLeft[i + leftDelay] = lSample;

				if (i + rightDelay < len)
					outputRight[i + rightDelay] = rSample;
			}

			for (int i = 0; i < len; i++)
			{
				var window = GetWindow(i, impulseLength, windowLen, windowType);
				outputLeft[i] *= window;
				outputRight[i] *= window;
			}

			return new[] { outputLeft, outputRight };
		}

		public static double GetWindow(int i, int signalLength, double windowLen, WindowMethod windowType)
		{
			var pos = i / (double)signalLength;
			if (pos < 1 - windowLen)
				return 1.0;
			if (i >= signalLength)
				return 0.0;
			if (windowLen < 0.002) // no effect of tiny window
				return 1.0;

			var posInWindow = (pos - (1 - windowLen)) / windowLen;
			if (windowType == WindowMethod.Truncate)
				return 0.0;
			if (windowType == WindowMethod.Linear)
				return 1 - posInWindow;
			if (windowType == WindowMethod.Logarithmic)
				return AudioLib.Utils.DB2gain(-posInWindow * 60);
			if (windowType == WindowMethod.Cosine)
				return (Math.Cos(posInWindow * Math.PI) + 1) * 0.5;

			return 1.0;
		}
	}
}

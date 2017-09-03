using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using LowProfile.Fourier.Double;

namespace ImpulseHd
{
	public class StereoEnhancerProcessor
	{
		private readonly MixingConfig config;
		private readonly double samplerate;
		private const int SignalLen = ImpulseConfig.MaxSampleLength;
		private readonly Transform fftTransform;
		private Complex[] fftSignalLeft;
		private Complex[] fftSignalRight;
		
		public StereoEnhancerProcessor(MixingConfig config, double samplerate)
		{
			this.config = config;
			this.samplerate = samplerate;
			this.fftTransform = new Transform(SignalLen);
		}

		public double[][] Process(double[][] input)
		{
			var inputLeft = input[0].Select(x => (Complex)x).Concat(new Complex[SignalLen - input[0].Length]).ToArray();
			var inputRight = input[1].Select(x => (Complex)x).Concat(new Complex[SignalLen - input[1].Length]).ToArray();

			fftSignalLeft = new Complex[inputLeft.Length];
			fftSignalRight = new Complex[inputRight.Length];
			fftTransform.FFT(inputLeft, fftSignalLeft);
			fftTransform.FFT(inputRight, fftSignalRight);
			var bands = GetBands();
			ProcessEqBands(bands);
			ProcessPhaseBands(bands);

			var outputFinal = new Complex[SignalLen];

			fftTransform.IFFT(fftSignalLeft, outputFinal);
			var leftOutput = outputFinal.Select(x => x.Real).ToArray();

			fftTransform.IFFT(fftSignalRight, outputFinal);
			var rightOutput = outputFinal.Select(x => x.Real).ToArray();

			ProcessBlend(leftOutput, rightOutput);

			return new[] { leftOutput, rightOutput };
		}

		private void ProcessBlend(double[] left, double[] right)
		{
			var blendGain = Utils.DB2gain(config.BlendAmountTransformed);
			if (config.BlendAmount == 0)
				blendGain = 0;

			for (int i = 0; i < left.Length; i++)
			{
				var l = left[i] + right[i] * blendGain;
				var r = right[i] + left[i] * blendGain;
				left[i] = l;
				right[i] = r;
			}
		}

		private void ProcessEqBands(int[] bandMap)
		{
			var nyquist = samplerate / 2.0;
			var hzPerBand = nyquist / (SignalLen / 2.0);
			var octSmooth = config.EqSmoothingOctavesTransformed;

			var gainsLeft = new double[fftSignalLeft.Length / 2 + 1];
			var gainsRight = new double[fftSignalLeft.Length / 2 + 1];

			for (int i = 1; i <= SignalLen / 2; i++)
			{
				var band = bandMap[i];
				var gain = config.StereoEq[band] * 2 - 1;
				double gainLeft;
				double gainRight;

				if (gain > 0)
				{
					var dbLeft = config.EqDepthDbTransformed * Math.Abs(gain);
					var dbRight = -dbLeft;
					gainLeft = Utils.DB2gain(dbLeft);
					gainRight = Utils.DB2gain(dbRight);
				}
				else
				{
					var dbRight = config.EqDepthDbTransformed * Math.Abs(gain);
					var dbLeft = -dbRight;
					gainLeft = Utils.DB2gain(dbLeft);
					gainRight = Utils.DB2gain(dbRight);
				}

				gainsLeft[i] = gainLeft;
				gainsRight[i] = gainRight;
			}

			// smoothing
			for (int i = 1; i <= SignalLen / 2; i++)
			{
				var hz = i * hzPerBand;
				var hztoSmooth = octSmooth * hz;
				var bandsToSmooth = (int)Math.Round(hztoSmooth / hzPerBand / 2.0);

				var gainLeft = 0.0;
				var gainRight = 0.0;
				int count = 0;

				for (int j = -bandsToSmooth; j <= bandsToSmooth; j++)
				{
					var idx = i + j;
					if (idx <= 0 || idx >= gainsLeft.Length)
						continue;

					gainLeft += gainsLeft[idx];
					gainRight += gainsRight[idx];
					count++;
				}

				gainLeft /= count;
				gainRight /= count;

				fftSignalLeft[i] *= (Complex)gainLeft;
				fftSignalLeft[fftSignalLeft.Length - i] *= (Complex)gainLeft;

				fftSignalRight[i] *= (Complex)gainRight;
				fftSignalRight[fftSignalRight.Length - i] *= (Complex)gainRight;
			}

		}

		private void ProcessPhaseBands(int[] bandMap)
		{
			var delaySamples = config.DelayMillisTransformed / 1000 * samplerate;

			for (int i = 1; i <= SignalLen / 2; i++)
			{
				var band = bandMap[i];
				var delay = config.StereoPhase[band] * 2 - 1;
				double delaySamplesLeft = 0;
				double delaySamplesRight = 0;

				if (delay < 0)
					delaySamplesLeft = delaySamples * Math.Abs(delay);
				else
					delaySamplesRight = delaySamples * Math.Abs(delay);

				var amountLeft = delaySamplesLeft / (double)ImpulseConfig.MaxSampleLength;
				var newValLeft = fftSignalLeft[i] * Complex.CExp(-2 * Math.PI * i * amountLeft);
				fftSignalLeft[i] = newValLeft;
				fftSignalLeft[fftSignalLeft.Length - i].Arg = -newValLeft.Arg;

				var amountRight = delaySamplesRight / (double)ImpulseConfig.MaxSampleLength;
				var newValRight = fftSignalRight[i] * Complex.CExp(-2 * Math.PI * i * amountRight);
				fftSignalRight[i] = newValRight;
				fftSignalRight[fftSignalRight.Length - i].Arg = -newValRight.Arg;
			}
		}

		private int[] GetBands()
		{
			var nyquist = samplerate / 2.0;
			var hzPerBand = nyquist / (SignalLen / 2.0);

			var bandsHz = config.GetCenterFrequencies();

			var output = new int[SignalLen];
			for (int i = 1; i < SignalLen / 2; i++)
			{
				var partialFreq = hzPerBand * i;

				// find the band closest to the current partial
				double minDist = 99999999999.0;
				int closestBand = -1;
				for (int j = 0; j < bandsHz.Length; j++)
				{
					var bandFreq = bandsHz[j];
					if (Math.Abs(bandFreq - partialFreq) < minDist)
					{
						closestBand = j;
						minDist = Math.Abs(bandFreq - partialFreq);
					}
				}

				output[i] = closestBand;
				output[SignalLen - i] = closestBand;
			}

			return output;
		}
	}
}

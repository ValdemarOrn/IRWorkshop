using LowProfile.Fourier.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class ImpulseConfigProcessor
	{
		private readonly ImpulseConfig config;
		private readonly Complex[] fftSignal;
		private readonly Transform fftTransform;

		private readonly int sampleCount;
		private readonly double samplerate;

		public ImpulseConfigProcessor(ImpulseConfig config)
		{
			this.config = config;
			sampleCount = ImpulseConfig.MaxSampleLength;
			samplerate = config.Samplerate;

			if (config.RawSampleData == null)
				config.LoadSampleData();

			this.fftTransform = new Transform(sampleCount);
			var input = config.RawSampleData.Select(x => (Complex)x).ToArray();

			fftSignal = new Complex[input.Length];
			fftTransform.FFT(input, fftSignal);
		}

		public Complex[] FftSignal => fftSignal;

		public double[] TimeSignal
		{
			get
			{
				var outputFinal = new Complex[sampleCount];
				fftTransform.IFFT(fftSignal, outputFinal);
				return outputFinal.Select(x => x.Real).ToArray();
			}
		}

		public SpectrumStage[] Stages => config.SpectrumStages.ToArray();

		public void ProcessStage(SpectrumStage stage)
		{
			// todo: fill in features

			if (!stage.IsEnabled)
				return;

			var nyquist = samplerate / 2;
			var absMaxIndex = fftSignal.Length / 2;
			var minIndex = (int)Math.Round(stage.MinFreqTransformed / (double)nyquist * absMaxIndex);
			var maxIndex = (int)Math.Round(stage.MaxFreqTransformed / (double)nyquist * absMaxIndex);

			if (minIndex < 1) minIndex = 1;
			if (maxIndex < 1) maxIndex = 1;
			if (minIndex >= absMaxIndex) minIndex = absMaxIndex;
			if (maxIndex >= absMaxIndex) maxIndex = absMaxIndex;

			for (int i = minIndex; i <= maxIndex; i++)
			{
				var newVal = fftSignal[i] * Complex.CExp(-2 * Math.PI * i * stage.DelaySamplesTransformed / (double)ImpulseConfig.MaxSampleLength);
				fftSignal[i] = newVal;
				fftSignal[fftSignal.Length - i].Arg = -newVal.Arg;
			}
		}

		public double[][] ProcessOutputStage()
		{
			// todo: process output properly
			return new[] { TimeSignal.Select(x => x).ToArray(), TimeSignal.Select(x => x).ToArray() };
		}
	}
}

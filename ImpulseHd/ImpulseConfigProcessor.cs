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

			var strengths = GetStrengths(stage);
			ProcessGain(stage, strengths);
			ProcessDelay(stage, strengths);
		}

		private void ProcessDelay(SpectrumStage stage, Strengths strengths)
		{
			for (int i = strengths.FMin; i <= strengths.FMax; i++)
			{
				var amount = stage.DelaySamplesTransformed / (double)ImpulseConfig.MaxSampleLength;
				amount = amount * strengths.Strength[i];
				var newVal = fftSignal[i] * Complex.CExp(-2 * Math.PI * i * amount);
				fftSignal[i] = newVal;
				fftSignal[fftSignal.Length - i].Arg = -newVal.Arg;
			}
		}

		private void ProcessGain(SpectrumStage stage, Strengths strengths)
		{
			for (int i = strengths.FMin; i <= strengths.FMax; i++)
			{
				var amount = AudioLib.Utils.DB2gain(stage.GainTransformed * strengths.Strength[i]);
				fftSignal[i] *= (Complex)amount;
				fftSignal[fftSignal.Length - i] *= (Complex)amount;
			}
		}

		private Strengths GetStrengths(SpectrumStage stage)
		{
			var nyquist = samplerate / 2;
			var absMaxIndex = fftSignal.Length / 2;
			var fMin = Math.Round(stage.MinFreqTransformed / (double)nyquist * absMaxIndex);
			var fMax = Math.Round(stage.MaxFreqTransformed / (double)nyquist * absMaxIndex);

			if (stage.MinFreqTransformed >= stage.MaxFreqTransformed)
				return new Strengths {FMax = 1, FMin = 1, Strength = new double[(int)nyquist]};

			if (fMin < 1) fMin = 1;
			if (fMax < 1) fMax = 1;
			if (fMin >= absMaxIndex) fMin = absMaxIndex;
			if (fMax >= absMaxIndex) fMax = absMaxIndex;

			var fBlendMin = Math.Round(Math.Pow(2, stage.LowBlendOctsTransformed) * stage.MinFreqTransformed / (double)nyquist * absMaxIndex);
			var fBlendMax = Math.Round(Math.Pow(2, -stage.HighBlendOctsTransformed) * stage.MaxFreqTransformed / (double)nyquist * absMaxIndex);

			if (fBlendMin < 1) fBlendMin = 1;
			if (fBlendMax < 1) fBlendMax = 1;
			if (fBlendMin >= absMaxIndex) fBlendMin = absMaxIndex;
			if (fBlendMax >= absMaxIndex) fBlendMax = absMaxIndex;

			var blendIn = new double[(int)nyquist];
			for (int i = (int)fMin; i <= (int)fMax; i++)
			{
				var octaveDistance = (i - fMin) / (fBlendMin - fMin) * stage.LowBlendOctsTransformed;
				var value = (Math.Log(1 + octaveDistance, 2)) / (Math.Log(1 + stage.LowBlendOctsTransformed, 2));

				if (fBlendMin == fMin)
					blendIn[i] = 1.0;
				else if (i <= fBlendMin)
					blendIn[i] = value;
				else
					blendIn[i] = 1.0;
			}

			var blendOut = new double[(int)nyquist];
			for (int i = (int)fMin; i <= (int)fMax; i++)
			{
				var octaveDistance = (i - fBlendMax) / (fMax - fBlendMax) * stage.HighBlendOctsTransformed;
				var value = (Math.Log(1 + octaveDistance)) / (Math.Log(1 + stage.HighBlendOctsTransformed));

				if (fMax == fBlendMax)
					blendOut[i] = 0.0;
				else if (i <= fBlendMax)
					blendOut[i] = 0;
				else
					blendOut[i] = value;
			}

			// mix the two together so that out cuts against in
			for (int i = 0; i < blendIn.Length; i++)
			{
				blendIn[i] = blendIn[i] - blendOut[i];
				if (blendIn[i] < 0)
					blendIn[i] = 0;
			}

			return new Strengths { FMin = (int)fMin, FMax = (int)fMax, Strength = blendIn };
		}

		class Strengths
		{
			public int FMin { get; set; }
			public int FMax { get; set; }
			public double[] Strength { get; set; }
		}

		public double[][] ProcessOutputStage()
		{
			// todo: process output properly
			return new[] { TimeSignal.Select(x => x).ToArray(), TimeSignal.Select(x => x).ToArray() };
		}
	}
}

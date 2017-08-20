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
			ProcessGainVariation(stage, strengths);
		}

		private void ProcessGainVariation(SpectrumStage stage, Strengths strengths)
		{
			var absMaxIndex = fftSignal.Length / 2;
			var gain = new double[strengths.Strength.Length];
			var amt = stage.GainSmoothingAmountTransformed;
			var octavesToSmooth = stage.GainSmoothingOctavesTransformed;
			var hzPerPartial = 1 / (double)absMaxIndex * samplerate / 2;

			for (int i = 1; i <= fftSignal.Length / 2; i++)
			{
				var freq = i * hzPerPartial;
				var lowFreq = freq * Math.Pow(2, -octavesToSmooth);
				var highFreq = freq * Math.Pow(2, octavesToSmooth);
				var iBelow = (freq - lowFreq) / hzPerPartial;
				var iAbove = (highFreq - freq) / hzPerPartial;
				
				var avgSum = 0.0;
				int count = 0;
				for (int j = -(int)Math.Round(iBelow); j <= Math.Round(iAbove); j++)
				{
					var idx = i + j;
					if (idx <= 0) continue;
					if (idx > absMaxIndex) continue;
					count++;
					var sample = fftSignal[idx].Abs;
					avgSum += sample;
				}

				avgSum /= count;
				var avgDb = AudioLib.Utils.Gain2DB(avgSum);
				var partialDb = AudioLib.Utils.Gain2DB(fftSignal[i].Abs);
				var diffDb = partialDb - avgDb;

				var stren = strengths.Strength[i];
				var newMagDb = avgDb + diffDb * (amt * stren + 1 * (1-stren));
				gain[i] = AudioLib.Utils.DB2gain(newMagDb) / fftSignal[i].Abs;
			}

			for (int i = strengths.FMin; i <= strengths.FMax; i++)
			{
				var g = gain[i];
				fftSignal[i] *= (Complex)g;
				fftSignal[fftSignal.Length - i] *= (Complex)g;
			}

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
				return new Strengths {FMax = 1, FMin = 1, Strength = new double[absMaxIndex + 1] };

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

			var blendIn = new double[absMaxIndex + 1];
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

			var blendOut = new double[absMaxIndex + 1];
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

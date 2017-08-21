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
			ProcessGainVariation(stage, strengths);
			ProcessRandomGain(stage, strengths);

			ProcessMinimumPhase(stage, strengths);

			ProcessDelay(stage, strengths);
		}

		private void ProcessRandomGain(SpectrumStage stage, Strengths strengths)
		{
			var rand = new Random(0);
			for (int i = 0; i < stage.RandomGainSeedTransformed; i++)
				rand.NextDouble(); // pop off x number of samples, instead of re-seeding, we "move" the sequence forward
			
			var filterCount = stage.RandomGainFilteringTransformed;
			var gainAmount = stage.RandomGainAmountTransformed;
			var randCount = strengths.Strength.Length + 2 * filterCount;
			var mode = stage.RandomGainModeTransformed;
			var skew = stage.RandomSkewAmountTransformed;
			var noise = Enumerable.Range(0, randCount).Select(x => rand.NextDouble() * 2 - 1).ToArray();
			var filteredNoise = new double[strengths.Strength.Length];

			for (int i = filterCount; i < noise.Length - filterCount; i++)
			{
				var sum = 0.0;
				for (int j = -filterCount; j <= filterCount; j++)
				{
					var idx = i + j;
					sum += noise[idx];
				}

				filteredNoise[i-filterCount] = sum / Math.Sqrt(2 * filterCount + 1);
			}
			
			for (int i = strengths.FMin; i <= strengths.FMax; i++)
			{
				var skewedNoise = Math.Pow(Math.Abs(filteredNoise[i]), skew) * Math.Sign(filteredNoise[i]);
				var gf = gainAmount * skewedNoise;

				var dbGain = gf * strengths.Strength[i];
				var scaler = AudioLib.Utils.DB2gain(dbGain);

				if (mode == ApplyMode.Amplify && scaler < 1)
					scaler = 1;
				if (mode == ApplyMode.Reduce && scaler > 1)
					scaler = 1;
				fftSignal[i] *= (Complex)scaler;
				fftSignal[fftSignal.Length - i] *= (Complex)scaler;
			}

		}

		private Complex[] Hilbert(double[] xr)
		{
			/* Matlab prototype - use "type hilbert" to get the code for the function in matlab!
			function output = myhilb(xr)
			n = size(xr,1);
			if (n == 1)
				xr = xr';
				n = size(xr,1);
			end

			x = fft(xr,n); % n-point FFT over columns.
			h  = zeros(n,1); % nx1 for nonempty. 0x0 for empty.

			if mod(n, 2) == 0
			  % even and nonempty
			  h([1 n/2+1]) = 1;
			  h(2:n/2) = 2;
			else
			  % odd and nonempty
			  h(1) = 1;
			  h(2:(n+1)/2) = 2;
			end

			xh = x.*h;
			x = ifft(xh);

			% Convert back to the original shape.
			%x = shiftdim(x,-nshifts);
			x = conj(x)';
			output = x;
			end

			*/
			
			var n = xr.Length;
			var transform = new Transform(n);
			var xrc = xr.Select(xx => (Complex)xx).ToArray();
			var x = new Complex[xrc.Length];
			transform.FFT(xrc, x); // n-point FFT over columns.
			var h = new double[n]; // nx1 for nonempty. 0x0 for empty.

			if (n % 2 == 0)
			{ 
				// even and nonempty
				h[0] = 1;
				h[n / 2] = 1;
				for (int i = 1; i < n/2; i++)
					h[i] = 2;
			}
			else
			{ 
			  // odd and nonempty
				h[0] = 1;
				for (int i = 1; i <= n / 2; i++) // NOT TEST NOT SURE CORRECT!
					h[i] = 2;
			}

			var xh = new Complex[x.Length];
			for (int i = 0; i < xh.Length; i++)
				xh[i] = x[i] * (Complex)h[i];
			
			transform.IFFT(xh, x);

			// Convert back to the original shape.
			// some reason I this I didn't have to apply this step to get the identical result... wat?!
			//for (int i = 0; i < xh.Length; i++)
			//	x[i].Imag = -x[i].Imag;

			return x;
		}

		private void ProcessMinimumPhase(SpectrumStage stage, Strengths strengths)
		{
			// https://dsp.stackexchange.com/questions/7872/derive-minimum-phase-from-magnitude
			// https://ccrma.stanford.edu/~jos/sasp/Minimum_Phase_Filter_Design.html
			// https://uk.mathworks.com/matlabcentral/newsreader/view_thread/17748
			// https://stackoverflow.com/questions/11942855/matlab-hilbert-transform-in-c

			/* Matlab example that achieves what we want:
				t=-15:0.25:15;
				x = pi*t+j*1e-9;
				h = real(sin(x)./x); % simple linear phase FIR
				H = fft(h,128);
				reaLogH = log(abs(H));
				hilb = hilbert(reaLogH);
				H2 = exp(hilb);
				hMinPhase = real(fliplr(ifft(H2)));
				hMinPhase = hMinPhase(1:length(h));

				figure(1)
				plot(t,h,t,hMinPhase)
				xlabel('time')
				ylabel('Amplitude')
			*/

			// hilbert function is the only problematic one, but I've translated that over to C#
			// This is still pretty much numbers voodoo to me, could someone in the fucking field write up a pragmatic description 
			// of just what the fuck the hilbert transform does without using pages and pages of obscure math?
			// fuck I hate academics...

			if (!stage.MinimumPhase)
				return;

			var H = fftSignal;
			var reaLogH = new double[H.Length];
			for (int i = 0; i < fftSignal.Length; i++)
			{
				reaLogH[i] = Math.Log(H[i].Abs);
			}

			var hilb = Hilbert(reaLogH);
			var H2 = hilb // translate over to System.Numerics as I don't have a Complex.Exp(Complex) function in my library... fail on me
				.Select(x => new System.Numerics.Complex(x.Real, x.Imag))
				.Select(x => System.Numerics.Complex.Exp(x))
				.Select(x => new Complex(x.Real, x.Imaginary))
				.ToArray();

			for (int i = 0; i < fftSignal.Length; i++)
			{
				fftSignal[i] = H2[i];
			}
		}

		private void ProcessGainVariation(SpectrumStage stage, Strengths strengths)
		{
			var absMaxIndex = fftSignal.Length / 2;
			var gain = new double[strengths.Strength.Length];
			var amt = stage.GainSmoothingAmountTransformed;
			var octavesToSmooth = stage.GainSmoothingOctavesTransformed;
			var hzPerPartial = 1 / (double)absMaxIndex * samplerate / 2;
			var mode = stage.GainSmoothingModeTransformed;

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
				var newGain = AudioLib.Utils.DB2gain(newMagDb) / fftSignal[i].Abs;
				gain[i] = newGain;

				if (mode == ApplyMode.Amplify && newGain < 1)
					gain[i] = 1;
				else if (mode == ApplyMode.Reduce && newGain > 1)
					gain[i] = 1;
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

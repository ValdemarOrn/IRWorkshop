using LowProfile.Fourier.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using AudioLib.Modules;
using AudioLib.TF;

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

			if (!config.SampleDataFromFileLoaded)
				config.LoadDataFromFile();

			config.ConvertSampleData();

			this.fftTransform = new Transform(sampleCount);
			var input = config.ConvertedSampleData.Select(x => (Complex)x).ToArray();

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
			if (!stage.IsEnabled)
				return;

			var strengths = GetStrengths(stage);
			ProcessGain(stage, strengths);
			ProcessGainVariation(stage, strengths);
			ProcessRandomGain(stage, strengths);
			ProcessorFrequencySkew(stage, strengths);

			ProcessMinimumPhase(stage, strengths);

			ProcessDelay(stage, strengths);
			ProcessPhaseBands(stage, strengths);
		}

		private void ProcessPhaseBands(SpectrumStage stage, Strengths strengths)
		{
			var bands = stage.PhaseBandsTransformed;
			var shift = stage.PhaseBandFreqShiftTransformed;
			var sections = GetBands(bands, shift, samplerate, FftSignal.Length);
			var rand = new Random(stage.PhaseBandSeedTransformed);
			int pb = 0;
			foreach (var section in sections)
			{
				var delaySamples = rand.NextDouble() * stage.PhaseBandDelayAmountTransformed;
				var k = pb / (double)(sections.Count - 1);
				var amountOfTracking = Math.Abs(stage.PhaseBandFreqTrackTransformed);
				if (stage.PhaseBandFreqTrackTransformed < 0)
				{
					k = 1 - k;
					var track = k * amountOfTracking + (1 - amountOfTracking);
					delaySamples *= track;
				}
				else
				{
					var track = k * amountOfTracking + (1 - amountOfTracking);
					delaySamples *= track;
				}

				var amount = delaySamples / (double)ImpulseConfig.MaxSampleLength;
				for (int i = section[0]; i <= section[1]; i++)
				{
					amount = amount * strengths.Strength[i];
					var newVal = fftSignal[i] * Complex.CExp(-2 * Math.PI * i * amount);
					fftSignal[i] = newVal;
					fftSignal[fftSignal.Length - i].Arg = -newVal.Arg;
				}

				pb++;
			}
		}

		private IList<int[]> GetBands(int bands, double shift, double samplerate, int fftSize)
		{
			var nyquist = samplerate / 2.0;
			var hzPerBand =  nyquist / (fftSize / 2.0);

			// generates nicely distributed intervals between 80hz and 10240 hz
			var bandRangesHz = Utils.Linspace(0, 7, bands + 1).Select(x => Math.Pow(2, x) * 80 * shift).Skip(1).ToArray();
			// replace last band with max frequency to affect the remaining treble range
			bandRangesHz[bandRangesHz.Length - 1] = nyquist;

			var lowBand = 1;
			var output = new List<int[]>();
			foreach (var b in bandRangesHz)
			{
				var highBand = (int)Math.Round(b / hzPerBand);
				var pair = new[] {lowBand, highBand};
				output.Add(pair);
				lowBand = highBand + 1;
			}

			return output;
		}

		private void ProcessorFrequencySkew(SpectrumStage stage, Strengths strengths)
		{
			var scaler = stage.FrequencySkewTransformed;
			var pinToTop = stage.PinToHighFrequency;
			var magnitude = fftSignal.Select(x => x.Abs).ToArray();
			
			for (int i = strengths.FMin; i <= strengths.FMax; i++)
			{
				double k;
				if (pinToTop)
				{
					k = strengths.FMax - (strengths.FMax - i) * scaler;
					if (k < strengths.FMin)
						k = strengths.FMin;
				}
				else
				{
					k = (i - strengths.FMin) * scaler + strengths.FMin;
					if (k > strengths.FMax)
						k = strengths.FMax;
				}

				var interpolated = AudioLib.Interpolate.Spline(k, magnitude, false);
				fftSignal[i].Abs = interpolated;
			}
		}

		private void ProcessRandomGain(SpectrumStage stage, Strengths strengths)
		{
			var rand = new Random(stage.RandomGainSeedTransformed);
			for (int i = 0; i < stage.RandomGainShiftTransformed; i++)
				rand.NextDouble(); // pop off x number of samples, "shifting" the sequence forward
			
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
			var minFreq = stage.MinFreqTransformed;
			var maxFreq = stage.MaxFreqTransformed;

			// slight hack, because we used a fixed frequency range, we don't pin it relative to the sampling frequency, then the absolute highest frequency 
			// that can be selected is 22Khz (set in the MaxFreqTransformed getter) - IF this frequency is used, we stretch it up so that to the nyquist frequency
			// to prevent a jump at 22Khz when using high sampling frequencies
			if (Math.Abs(stage.MinFreqTransformed - ImpulseConfig.MaxFrequency) < 0.01)
				minFreq = nyquist;
			if (Math.Abs(stage.MaxFreqTransformed - ImpulseConfig.MaxFrequency) < 0.01)
				maxFreq = nyquist;

			var fMin = Math.Round(minFreq / (double)nyquist * absMaxIndex);
			var fMax = Math.Round(maxFreq / (double)nyquist * absMaxIndex);
			
			if (minFreq >= maxFreq)
				return new Strengths {FMax = 1, FMin = 1, Strength = new double[absMaxIndex + 1] };

			if (fMin < 1) fMin = 1;
			if (fMax < 1) fMax = 1;
			if (fMin >= absMaxIndex) fMin = absMaxIndex;
			if (fMax >= absMaxIndex) fMax = absMaxIndex;

			var fBlendMin = Math.Round(Math.Pow(2, stage.LowBlendOctsTransformed) * minFreq / (double)nyquist * absMaxIndex);
			var fBlendMax = Math.Round(Math.Pow(2, -stage.HighBlendOctsTransformed) * maxFreq / (double)nyquist * absMaxIndex);

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
	}
}

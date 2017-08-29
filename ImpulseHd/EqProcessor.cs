using AudioLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;

namespace ImpulseHd
{
	public class EqProcessor
	{
		private readonly MixingConfig config;
		private readonly double samplerate;
		private readonly Biquad[] filters;

		public EqProcessor(MixingConfig config, double samplerate)
		{
			this.config = config;
			this.samplerate = samplerate;

			filters = Enumerable.Range(0, 6)
				.Select(_ =>
				{
					var b = new Biquad
					{
						Frequency = 100,
						Slope = 1.0,
						GainDB = 0,
						Q = 1,
						Samplerate = samplerate,
						Type = Biquad.FilterType.Peak
					};
					b.Update();
					return b;
				})
				.ToArray();

			filters[0].Frequency = config.Eq1FreqTransformed;
			filters[0].Q = config.Eq1QTransformed;
			filters[0].GainDB = config.Eq1GainDbTransformed;

			filters[1].Frequency = config.Eq2FreqTransformed;
			filters[1].Q = config.Eq2QTransformed;
			filters[1].GainDB = config.Eq2GainDbTransformed;

			filters[2].Frequency = config.Eq3FreqTransformed;
			filters[2].Q = config.Eq3QTransformed;
			filters[2].GainDB = config.Eq3GainDbTransformed;

			filters[3].Frequency = config.Eq4FreqTransformed;
			filters[3].Q = config.Eq4QTransformed;
			filters[3].GainDB = config.Eq4GainDbTransformed;

			filters[4].Frequency = config.Eq5FreqTransformed;
			filters[4].Q = config.Eq5QTransformed;
			filters[4].GainDB = config.Eq5GainDbTransformed;

			filters[5].Frequency = config.Eq6FreqTransformed;
			filters[5].Q = config.Eq6QTransformed;
			filters[5].GainDB = config.Eq6GainDbTransformed;

			foreach (var f in filters)
				f.Update();
		}

		public Dictionary<double, double> GetFrequencyResponse()
		{
			var nyquist = samplerate / 2;
			var freqs = Enumerable.Range(0, 1000).Select(x => x / 1000.0).Select(x => Math.Pow(2, x * 10) * 20).ToArray();
			freqs = freqs.Select(x => x / nyquist * Math.PI).ToArray();

			var output = new Dictionary<double, double>();
			var responses = filters.Select(f => Freqz.Compute(f.B, f.A, freqs)).ToArray();

			foreach (var response in responses)
			{
				foreach (var f in response)
				{
					if (output.ContainsKey(f.W))
						output[f.W] *= f.Magnitude;
					else
						output[f.W] = f.Magnitude;
				}
			}

			return output.ToDictionary(x => x.Key / Math.PI * nyquist, x => x.Value);
		}

		public double[][] Process(double[][] input)
		{
			var len = input[0].Length;
			var leftOut = new double[len];
			var rightOut = new double[len];

			for (int i = 0; i < len; i++)
			{
				var sample = input[0][i];
				sample = filters[0].Process(sample);
				sample = filters[1].Process(sample);
				sample = filters[2].Process(sample);
				sample = filters[3].Process(sample);
				sample = filters[4].Process(sample);
				sample = filters[5].Process(sample);
				leftOut[i] = sample;
			}

			filters[0].ClearBuffers();
			filters[1].ClearBuffers();
			filters[2].ClearBuffers();
			filters[3].ClearBuffers();
			filters[4].ClearBuffers();
			filters[5].ClearBuffers();

			for (int i = 0; i < len; i++)
			{
				var sample = input[1][i];
				sample = filters[0].Process(sample);
				sample = filters[1].Process(sample);
				sample = filters[2].Process(sample);
				sample = filters[3].Process(sample);
				sample = filters[4].Process(sample);
				sample = filters[5].Process(sample);
				rightOut[i] = sample;
			}

			return new[] { leftOut, rightOut };
		}
	}
}

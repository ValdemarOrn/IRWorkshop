using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using Newtonsoft.Json;

namespace IrWorkshop
{
	public class ImpulseConfig
	{
		public const int MaxSampleLength = 8192;
		public const double MaxFrequency = 22000.0;

		public ImpulseConfig()
		{
			SpectrumStages = new [] { new SpectrumStage() };
			OutputStage = new OutputStage();
			Enable = true;
		}

		public int Index { get; set; }
		public string Name { get; set; }
		public double SampleStart { get; set; }
		public string FilePath { get; set; }
		public bool Enable { get; set; }
		public bool Solo { get; set; }
		public int Samplerate { get; set; }
		public int ImpulseLength { get; set; }

		public string NameWithIndex => $"{Index} - {Name}";

		[JsonIgnore]
		public double[][] SampleDataFromFile { get; set; }
		[JsonIgnore]
		public bool SampleDataFromFileLoaded { get; set; }
		[JsonIgnore]
		public int SamplerateFromFile { get; set; }

		public bool FileIsStereo => SampleDataFromFile != null && SampleDataFromFile.Length > 1;

		// use right channel of the input .wav file
		public bool UseRightChanel { get; set; }

		[JsonIgnore]
		public double[] ConvertedSampleData { get; set; }

		public SpectrumStage[] SpectrumStages { get; set; }
		public OutputStage OutputStage { get; set; }

		/// <summary>
		/// Loads the raw wav file data at the original samplerate into memory
		/// </summary>
		public void LoadDataFromFile()
		{
			if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
			{
				SampleDataFromFile = null;
				SamplerateFromFile = 0;
			}
			else
			{
				var format = WaveFiles.ReadWaveFormat(FilePath);
				SampleDataFromFile = WaveFiles.ReadWaveFile(FilePath);
				SamplerateFromFile = format.SampleRate;
				if (!FileIsStereo) UseRightChanel = false;
			}

			SampleDataFromFileLoaded = true;
		}

		/// <summary>
		/// Converts the .wav data from its original samplerate to the desired samplerate
		/// </summary>
		public void ConvertSampleData()
		{
			double[] waveData;
			int skipLen = 0;

			if (SampleDataFromFile == null)
			{
				// load pure impulse as fallback
				waveData = new[] { 1.0, 0.0, 0.0, 0.0 };
			}
			else
			{
				waveData = GetInterpolatedData(SampleDataFromFile[(FileIsStereo && UseRightChanel) ? 1 : 0], SamplerateFromFile, Samplerate);

				var len = waveData.Length;
				skipLen = (int)(SampleStart * len);
				if (skipLen >= len)
					skipLen = len - 1;
			}
			
			ConvertedSampleData = waveData.Skip(skipLen).Take(MaxSampleLength).ToArray();
			ConvertedSampleData = ConvertedSampleData.Concat(new double[MaxSampleLength - ConvertedSampleData.Length]).ToArray();
		}

		private double[] GetInterpolatedData(double[] data, int inputSamplerate, int desiredSamplerate)
		{
			if (inputSamplerate == desiredSamplerate)
				return data.ToArray();

			data = data.Concat(new[] { 0.0, 0.0 }).ToArray(); // to make sure interpolation can work at the very last samples
			var factor = inputSamplerate / (double)desiredSamplerate;
			var output = new List<double>();
			double idx = 0.0;
			while (true)
			{
				if (idx >= data.Length - 2)
					break;

				var sample = Interpolate.Spline(idx, data, false);
				output.Add(sample);
				idx += factor;
			}

			return output.ToArray();
		}
	}
	
	public enum WindowMethod
	{
		Truncate,
		Linear,
		Logarithmic,
		Cosine,
	}

	public enum FreqSkewMode
	{
		Skew, // Gradually shifts bands up or down from the middle of the band range
		Zero, // If skew < 1, leaves "empty" bands at the top or bottom
		Move, // instead of stretching, moves the entire range up or down
	}

	public enum ApplyMode
	{
		Reduce = -1,
		Bipolar = 0,
		Amplify = 1
	};
}

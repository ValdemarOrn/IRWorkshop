using AudioLib;
using LowProfile.Fourier;
using LowProfile.Fourier.Double;
using LowProfile.Visuals;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
    class Program
    {

        const int size = 512;
        static Random rand = new Random();

        [STAThread]
        static void Main(string[] args)
        {
			var file = @"c:\test.wav";
			var rand = new Random();
			var testSnd = Enumerable.Range(0, 500).Select(x => rand.NextDouble() * 2 - 1).Concat(new double[3500]).ToArray();
			WaveFiles.WriteWaveFile(new double[][] { testSnd }, WaveFiles.WaveFormat.PCM24Bit, 48000, file);

			var config = new ImpulseConfig
			{
				FilePath = file,
				OutputStage = { Gain = 1 },
				SampleSize = 8192,
				Samplerate = 48000,
			};

			var stage = config.SpectrumStages[0];
			stage.IsEnabled = true;
			stage.MinFreq = 0;
			stage.MaxFreq = 99999;
			stage.DelaySamples = 500;

			stage = config.SpectrumStages[1];
			stage.IsEnabled = true;
			stage.MinFreq = 1000;
			stage.MaxFreq = 20000;
			stage.DelaySamples = 1000;


			var proc = new ImpulseProcessor(config);

			foreach (var st in proc.Stages)
			{
				proc.ProcessStage(st);
				var response = proc.FftSignal;
				var pm = new PlotModel();
				pm.AddLine(proc.TimeSignal);
				pm.Show();
				//PlotFft(pm, response);
			}



            /*var file = @"C:\2off-pres5.wav";
            var wav2d = AudioLib.WaveFiles.ReadWaveFile(file);
            var wav = wav2d[0].ToList();
            while (wav.Count < size)
                wav.Add(0.0);

            var tf = new Transform(size);
            var input = wav.Select(x => (Complex)x).ToArray();

            var fftSignal = new Complex[input.Length];
            tf.FFT(input, fftSignal);
            var outputFinal = new Complex[size];
            tf.IFFT(fftSignal, outputFinal);
            WaveFiles.WriteWaveFile(new double[1][] { outputFinal.Select(x => x.Real).ToArray() }, WaveFiles.WaveFormat.PCM24Bit, 44100, @"c:\Orig.wav");


            Process(tf, input, @"c:\IR1.wav");
            Process(tf, input, @"c:\IR2.wav");*/
        }

		private static void PlotFft(PlotModel pm, Complex[] response)
		{
			pm.Axes.Add(new OxyPlot.Axes.LogarithmicAxis { Position = OxyPlot.Axes.AxisPosition.Bottom });
			var left = new OxyPlot.Axes.LogarithmicAxis { Position = OxyPlot.Axes.AxisPosition.Left, Key = "Left" };
			var right = new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Right, Key = "Right" };
			pm.Axes.Add(left);
			pm.Axes.Add(right);
			var abs = pm.AddLine(response.Select(x => x.Abs));
			var arg = pm.AddLine(response.Select(x => x.Arg));
			abs.YAxisKey = "Left";
			arg.YAxisKey = "Right";
			pm.Show();
		}

		/* private static void Process(Transform tf, Complex[] input, string output)
		 {
			 var fftSignal = new Complex[input.Length];
			 var randval = Math.PI * 2.4;

			 tf.FFT(input, fftSignal);
			 for (int i = 1; i < fftSignal.Length / 2; i++)
			 {
				 //if (i % 100 == 1)
					 //randval = rand.NextDouble();

				 var val = fftSignal[i].Arg + randval * 1;
				 fftSignal[i].Arg = 0;
				 fftSignal[fftSignal.Length - i].Arg = -val;
			 }

			 var outputFinal = new Complex[size];
			 tf.IFFT(fftSignal, outputFinal);
			 outputFinal = outputFinal.Skip(size / 2).Concat(outputFinal.Take(size / 2)).ToArray();
			 WaveFiles.WriteWaveFile(new double[1][] { outputFinal.Select(x => x.Real).ToArray() }, WaveFiles.WaveFormat.PCM24Bit, 44100, output);
		 }*/
	}
}

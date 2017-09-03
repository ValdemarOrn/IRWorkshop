using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AudioLib;

namespace IrWorkshop.Ui
{
    class Program
    {
		[STAThread]
	    public static void Main(string[] args)
		{
			if (args.Length <= 1)
			{
				var app = new Application();
				var window = new MainWindow();
				if (args.Length == 1)
					window.ViewModel.LoadPreset(args[0]);
				app.MainWindow = window;
				app.MainWindow.Show();
				app.Run();
			}
			else
			{
				//var str = @"-apply Stereo -source "C:\Users\Valdemar\Desktop\Impulse Presets\Stereo.irw" -dest "C:\Users\Valdemar\Desktop\Impulse Presets\Stereo1" ".Split(' ');
				//var str = @"-export -length 1024 -samplerate 48000 -source C:\Users\Valdemar\Desktop\ImpulsePresets\scratch -dest C:\Users\Valdemar\Desktop\ImpulsePresets\scratch\dest -overwrite".Split(' ');
				var dict = LowProfile.Core.Args.Parser.Parse(args);
				var mono = dict.ContainsKey("mono");

				if (dict.ContainsKey("apply"))
				{
					Apply(dict["apply"], dict["source"], dict["dest"]);
				}
				else if (dict.ContainsKey("export"))
				{
					Export(int.Parse(dict["length"]), int.Parse(dict["samplerate"]), dict["source"], dict["dest"], mono);
				}
			}
	    }

	    private static void Apply(string mode, string source, string dest)
	    {
			var sourceJson = File.ReadAllText(source);
		    var sourcePreset = Serializer.PresetSerializer.DeserializePreset(sourceJson);

			var presetFiles = Directory.GetFiles(dest, "*.irw");

		    foreach (var file in presetFiles)
		    {
			    var json = File.ReadAllText(file);
			    var preset = Serializer.PresetSerializer.DeserializePreset(json);

			    if (mode == "Stereo")
			    {
				    preset.MixingConfig.BlendAmount = sourcePreset.MixingConfig.BlendAmount;
				    preset.MixingConfig.DelayMillis = sourcePreset.MixingConfig.DelayMillis;
				    preset.MixingConfig.EqDepthDb = sourcePreset.MixingConfig.EqDepthDb;
				    preset.MixingConfig.EqSmoothingOctaves = sourcePreset.MixingConfig.EqSmoothingOctaves;
				    preset.MixingConfig.FreqShift = sourcePreset.MixingConfig.FreqShift;
				    preset.MixingConfig.StereoEq = sourcePreset.MixingConfig.StereoEq;
				    preset.MixingConfig.StereoPhase = sourcePreset.MixingConfig.StereoPhase;
				}
				else if (mode == "Eq")
			    {
					preset.MixingConfig.Eq1Freq = sourcePreset.MixingConfig.Eq1Freq;
				    preset.MixingConfig.Eq2Freq = sourcePreset.MixingConfig.Eq2Freq;
				    preset.MixingConfig.Eq3Freq = sourcePreset.MixingConfig.Eq3Freq;
				    preset.MixingConfig.Eq4Freq = sourcePreset.MixingConfig.Eq4Freq;
				    preset.MixingConfig.Eq5Freq = sourcePreset.MixingConfig.Eq5Freq;
				    preset.MixingConfig.Eq6Freq = sourcePreset.MixingConfig.Eq6Freq;

				    preset.MixingConfig.Eq1Q = sourcePreset.MixingConfig.Eq1Q;
				    preset.MixingConfig.Eq2Q = sourcePreset.MixingConfig.Eq2Q;
				    preset.MixingConfig.Eq3Q = sourcePreset.MixingConfig.Eq3Q;
				    preset.MixingConfig.Eq4Q = sourcePreset.MixingConfig.Eq4Q;
				    preset.MixingConfig.Eq5Q = sourcePreset.MixingConfig.Eq5Q;
				    preset.MixingConfig.Eq6Q = sourcePreset.MixingConfig.Eq6Q;

				    preset.MixingConfig.Eq1GainDb = sourcePreset.MixingConfig.Eq1GainDb;
				    preset.MixingConfig.Eq2GainDb = sourcePreset.MixingConfig.Eq2GainDb;
				    preset.MixingConfig.Eq3GainDb = sourcePreset.MixingConfig.Eq3GainDb;
				    preset.MixingConfig.Eq4GainDb = sourcePreset.MixingConfig.Eq4GainDb;
				    preset.MixingConfig.Eq5GainDb = sourcePreset.MixingConfig.Eq5GainDb;
				    preset.MixingConfig.Eq6GainDb = sourcePreset.MixingConfig.Eq6GainDb;
				}

			    var newJson = Serializer.PresetSerializer.SerializePreset(preset);
			    File.WriteAllText(file, newJson);
		    }
	    }

	    private static void Export(int length, int samplerate, string source, string dest, bool mono)
	    {
			var presetFiles = Directory.GetFiles(source, "*.irw");

		    foreach (var file in presetFiles)
		    {
			    var json = File.ReadAllText(file);
			    var preset = Serializer.PresetSerializer.DeserializePreset(json);
			    preset.SamplerateTransformed = samplerate;
				preset.ImpulseLengthTransformed = length;
				
			    var processor = new ImpulsePresetProcessor(preset);
			    var output = processor.Process();

			    var targetFile = Path.GetFileNameWithoutExtension(file) + ".wav";
			    targetFile = Path.Combine(dest, targetFile);

			    var targetLFile = Path.GetFileNameWithoutExtension(file) + "-L.wav";
			    targetLFile = Path.Combine(dest, targetLFile);

			    var targetRFile = Path.GetFileNameWithoutExtension(file) + "-R.wav";
			    targetRFile = Path.Combine(dest, targetRFile);

			    if (!Directory.Exists(dest))
				    Directory.CreateDirectory(dest);

				if (mono)
			    {
				    WaveFiles.WriteWaveFile(new[] { output[0] }, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, targetFile);
				}
			    else
			    {
					WaveFiles.WriteWaveFile(output, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, targetFile);
				    WaveFiles.WriteWaveFile(new[] { output[0] }, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, targetLFile);
				    WaveFiles.WriteWaveFile(new[] { output[1] }, WaveFiles.WaveFormat.PCM24Bit, preset.SamplerateTransformed, targetRFile);
				}
			}
		}
	}
}

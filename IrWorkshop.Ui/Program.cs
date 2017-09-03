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
				//var str = @"-apply EQ -source C:\Users\Valdemar\Desktop\Impulse Presets\scratch -dest C:\Users\Valdemar\Desktop\Impulse Presets\scratch\dest -overwrite".Split(' ');
				//var str = @"-export -length 1024 -samplerate 48000 -source C:\Users\Valdemar\Desktop\ImpulsePresets\scratch -dest C:\Users\Valdemar\Desktop\ImpulsePresets\scratch\dest -overwrite".Split(' ');
				var dict = LowProfile.Core.Args.Parser.Parse(args);
				var overwrite = dict.ContainsKey("overwrite");
				var mono = dict.ContainsKey("mono");

				if (dict.ContainsKey("apply"))
				{
					Apply(dict["apply"], dict["source"], dict["dest"], overwrite);
				}
				if (dict.ContainsKey("export"))
				{
					Export(int.Parse(dict["length"]), int.Parse(dict["samplerate"]), dict["source"], dict["dest"], mono, overwrite);
				}
			}
			

		    
	    }

	    private static void Apply(string mode, string source, string dest, bool overwrite)
	    {
		    

	    }

	    private static void Export(int length, int samplerate, string source, string dest, bool mono, bool overwrite)
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

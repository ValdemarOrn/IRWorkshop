<Query Kind="Statements" />

var files = Directory.GetFiles(@"C:\Src\_Tree\Audio\IrWorkshop\Wav\StereoWide", "*.wav", SearchOption.AllDirectories);

foreach (var file in files)
{
	if (file.Contains("-L.wav") || file.Contains("-R.wav"))
	{
		
	}
	else
	{
		var dir = Path.GetDirectoryName(file);
		dir += "-Stereo";
		Directory.CreateDirectory(dir);
		File.Move(file, Path.Combine(dir, Path.GetFileName(file)));
	}
}
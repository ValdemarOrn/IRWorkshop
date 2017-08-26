using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class ImpulsePresetProcessor
	{
		private readonly ImpulsePreset preset;

		public ImpulsePresetProcessor(ImpulsePreset preset)
		{
			this.preset = preset;
		}
		
		public double[][] Process()
		{
			var outputL = new double[preset.ImpulseLengthTransformed];
			var outputR = new double[preset.ImpulseLengthTransformed];
			var hasSolo = preset.ImpulseConfig.Any(x => x.Solo);
			var windowLen = preset.WindowLengthTransformed;
			var windowType = preset.WindowMethodTransformed;

			foreach (var impulse in preset.ImpulseConfig)
			{
				if (hasSolo && !impulse.Solo)
					continue;
				if (!impulse.Enable)
					continue;

				var processor = new ImpulseConfigProcessor(impulse);
				foreach(var stage in processor.Stages)
				{
					processor.ProcessStage(stage);
				}
				var stageOutput = processor.ProcessOutputStage();
				Sum(outputL, stageOutput[0]);
				Sum(outputR, stageOutput[1]);
			}

			for (int i = 0; i < outputL.Length; i++)
			{
				var window = ImpulseConfigProcessor.GetWindow(i, preset.ImpulseLengthTransformed, windowLen, windowType);
				outputL[i] *= window;
				outputR[i] *= window;
			}

			return new[] {outputL, outputR};
		}

		private void Sum(double[] outputL, double[] stageOutput)
		{
			for (int i = 0; i < outputL.Length && i < stageOutput.Length; i++)
			{
				outputL[i] = outputL[i] + stageOutput[i];
			}
		}
	}
}

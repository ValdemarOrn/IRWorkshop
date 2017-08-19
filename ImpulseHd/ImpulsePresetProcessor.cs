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
			var outputL = new double[preset.ImpulseLength];
			var outputR = new double[preset.ImpulseLength];

			foreach (var stage in preset.ImpulseConfig)
			{
				var processor = new ImpulseConfigProcessor(stage);
				var stageOutput = processor.ProcessOutputStage();
				Sum(outputL, stageOutput[0]);
				Sum(outputR, stageOutput[1]);
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

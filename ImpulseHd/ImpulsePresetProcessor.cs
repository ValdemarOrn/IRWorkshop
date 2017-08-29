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

			foreach (var impulseConfig in preset.ImpulseConfig)
			{
				if (hasSolo && !impulseConfig.Solo)
					continue;
				if (!impulseConfig.Enable)
					continue;

				var processor = new ImpulseConfigProcessor(impulseConfig);
				foreach(var stage in processor.Stages)
				{
					processor.ProcessStage(stage);
				}

				var outputProcessor = new OutputConfigProcessor(
					new[] { processor.TimeSignal, processor.TimeSignal }, 
					impulseConfig.OutputStage, 
					impulseConfig.ImpulseLength,
					impulseConfig.Samplerate);

				var stageOutput = outputProcessor.ProcessOutputStage();
				Sum(outputL, stageOutput[0]);
				Sum(outputR, stageOutput[1]);
			}

			var eqProcessor = new EqProcessor(preset.MixingConfig, preset.SamplerateTransformed);
			var output = eqProcessor.Process(new[] { outputL, outputR });

			var mixingOutputProcessor = new OutputConfigProcessor(
				output,
				preset.MixingConfig.OutputStage,
				preset.ImpulseLengthTransformed,
				preset.SamplerateTransformed);

			var lr = mixingOutputProcessor.ProcessOutputStage();
			return lr;
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

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
			var bufferLeft = new double[ImpulseConfig.MaxSampleLength];
			var bufferRight = new double[ImpulseConfig.MaxSampleLength];
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
				Sum(bufferLeft, stageOutput[0]);
				Sum(bufferRight, stageOutput[1]);
			}

			var eqProcessor = new EqProcessor(preset.MixingConfig, preset.SamplerateTransformed);
			var output = eqProcessor.Process(new[] { bufferLeft, bufferRight });

			var stereoProcessor = new StereoEnhancerProcessor(preset.MixingConfig, preset.SamplerateTransformed);
			output = stereoProcessor.Process(output);

			var mixingOutputProcessor = new OutputConfigProcessor(
				output,
				preset.MixingConfig.OutputStage,
				preset.ImpulseLengthTransformed,
				preset.SamplerateTransformed);

			output = mixingOutputProcessor.ProcessOutputStage();

			return new []
			{
				output[0].Take(preset.ImpulseLengthTransformed).ToArray(),
				output[1].Take(preset.ImpulseLengthTransformed).ToArray()
			};
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

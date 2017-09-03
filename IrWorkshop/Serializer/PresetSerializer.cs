using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IrWorkshop.Serializer
{
	public class PresetSerializer
	{
		class WritablePropertiesOnlyResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
				return props.Where(p => p.Writable).ToList();
			}
		}

		public static string SerializePreset(ImpulsePreset preset)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				ContractResolver = new WritablePropertiesOnlyResolver()
			};

			settings.Formatting = Formatting.Indented;

			string json = JsonConvert.SerializeObject(preset, settings);
			return json;
		}

		public static ImpulsePreset DeserializePreset(string json)
		{
			var preset = JsonConvert.DeserializeObject<ImpulsePreset>(json);
			foreach (var imp in preset.ImpulseConfig)
			{
				foreach (var spec in imp.SpectrumStages)
				{
					if (spec.SerializedSelectedApplySourceIndex != -1)
						spec.SelectedApplySource = preset.ImpulseConfig[spec.SerializedSelectedApplySourceIndex];
				}
			}
			return preset;
		}

		public static string SerializeImpulse(ImpulseConfig impulseConfig)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				ContractResolver = new WritablePropertiesOnlyResolver()
			};

			settings.Formatting = Formatting.Indented;

			string json = JsonConvert.SerializeObject(impulseConfig, settings);
			return json;
		}

		public static ImpulseConfig DeserializeImpulse(string json)
		{
			var preset = JsonConvert.DeserializeObject<ImpulseConfig>(json);
			return preset;
		}
	}
}

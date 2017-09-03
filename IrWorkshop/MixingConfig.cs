using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;

namespace IrWorkshop
{
	public class MixingConfig
	{
		public const int BandCount = 16;

		public MixingConfig()
		{
			OutputStage = new OutputStage();

			Eq1Freq = 0.5;
			Eq2Freq = 0.5;
			Eq3Freq = 0.5;
			Eq4Freq = 0.5;
			Eq5Freq = 0.5;
			Eq6Freq = 0.5;

			Eq1Q = 0.5;
			Eq2Q = 0.5;
			Eq3Q = 0.5;
			Eq4Q = 0.5;
			Eq5Q = 0.5;
			Eq6Q = 0.5;

			Eq1GainDb = 0.5;
			Eq2GainDb = 0.5;
			Eq3GainDb = 0.5;
			Eq4GainDb = 0.5;
			Eq5GainDb = 0.5;
			Eq6GainDb = 0.5;

			EqDepthDb = 0.5;
			EqSmoothingOctaves = 0.5;
			DelayMillis = 0.5;
			FreqShift = 0.5;
			BlendAmount = 0.0;

			StereoEq = Utils.Linspace(0.5, 0.5, BandCount);
			StereoPhase = Utils.Linspace(0.5, 0.5, BandCount);
		}

		public OutputStage OutputStage { get; set; }

		// -------------------------------- Parameteric EQ Section --------------------------------

		public double Eq1Freq { get; set; }
		public double Eq2Freq { get; set; }
		public double Eq3Freq { get; set; }
		public double Eq4Freq { get; set; }
		public double Eq5Freq { get; set; }
		public double Eq6Freq { get; set; }

		public double Eq1Q { get; set; }
		public double Eq2Q { get; set; }
		public double Eq3Q { get; set; }
		public double Eq4Q { get; set; }
		public double Eq5Q { get; set; }
		public double Eq6Q { get; set; }

		public double Eq1GainDb { get; set; }
		public double Eq2GainDb { get; set; }
		public double Eq3GainDb { get; set; }
		public double Eq4GainDb { get; set; }
		public double Eq5GainDb { get; set; }
		public double Eq6GainDb { get; set; }

		public double Eq1FreqTransformed => 20 + ValueTables.Get(Eq1Freq, ValueTables.Response3Dec) * (22000 - 20);
		public double Eq2FreqTransformed => 20 + ValueTables.Get(Eq2Freq, ValueTables.Response3Dec) * (22000 - 20);
		public double Eq3FreqTransformed => 20 + ValueTables.Get(Eq3Freq, ValueTables.Response3Dec) * (22000 - 20);
		public double Eq4FreqTransformed => 20 + ValueTables.Get(Eq4Freq, ValueTables.Response3Dec) * (22000 - 20);
		public double Eq5FreqTransformed => 20 + ValueTables.Get(Eq5Freq, ValueTables.Response3Dec) * (22000 - 20);
		public double Eq6FreqTransformed => 20 + ValueTables.Get(Eq6Freq, ValueTables.Response3Dec) * (22000 - 20);

		public double Eq1QTransformed => Math.Pow(10, 2 * Eq1Q - 1);
		public double Eq2QTransformed => Math.Pow(10, 2 * Eq2Q - 1);
		public double Eq3QTransformed => Math.Pow(10, 2 * Eq3Q - 1);
		public double Eq4QTransformed => Math.Pow(10, 2 * Eq4Q - 1);
		public double Eq5QTransformed => Math.Pow(10, 2 * Eq5Q - 1);
		public double Eq6QTransformed => Math.Pow(10, 2 * Eq6Q - 1);

		public double Eq1GainDbTransformed => -20 + 40 * Eq1GainDb;
		public double Eq2GainDbTransformed => -20 + 40 * Eq2GainDb;
		public double Eq3GainDbTransformed => -20 + 40 * Eq3GainDb;
		public double Eq4GainDbTransformed => -20 + 40 * Eq4GainDb;
		public double Eq5GainDbTransformed => -20 + 40 * Eq5GainDb;
		public double Eq6GainDbTransformed => -20 + 40 * Eq6GainDb;

		// -------------------------------- Stereo section --------------------------------

		public double EqDepthDb { get; set; }
		public double EqSmoothingOctaves { get; set; }
		public double DelayMillis { get; set; }
		public double FreqShift { get; set; }
		public double BlendAmount { get; set; }

		public double EqDepthDbTransformed => 12 * EqDepthDb;
		public double EqSmoothingOctavesTransformed => ValueTables.Get(EqSmoothingOctaves, ValueTables.Response2Dec) * 2;
		public double DelayMillisTransformed => ValueTables.Get(DelayMillis, ValueTables.Response2Oct) * 80;
		public double FreqShiftTransformed => 0.5 + FreqShift;
		public double BlendAmountTransformed => -40 + 40 * BlendAmount;
		
		public double[] StereoEq { get; set; }
		public double[] StereoPhase { get; set; }

		public string[] Frequencies
		{
			get
			{
				var fs = GetCenterFrequencies();
				var output = new string[fs.Length];
				for (int i = 0; i < fs.Length; i++)
				{
					var f = fs[i];
					if (f < 1000)
						output[i] = $"{f:0}";
					else
						output[i] = $"{f/1000:0.0}k";
				}

				return output;
			}
		}

		public double[] GetCenterFrequencies()
		{
			var bands = 16;
			var bandRangesHz = Utils.Linspace(0, 7.4, bands).Select(x => Math.Pow(2, x) * 80).ToArray();
			return bandRangesHz.Select(x => x * FreqShiftTransformed).ToArray();
		}

	}
}

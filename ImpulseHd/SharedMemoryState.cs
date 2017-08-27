using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public class SharedMemoryState
	{
		public int Id;

		//public DateTime ClipTimeLeft;
		//public DateTime ClipTimeRight;

		public int SelectedInputLeft;
		public int SelectedInputRight;
		public int SelectedOutputLeft;
		public int SelectedOutputRight;

		public float Gain;
		public int IrLength;

		public float[] IrLeft;
		public float[] IrRight;

		public void Write(MemoryMappedViewAccessor accessor)
		{
			accessor.Write(0, Id);

			//accessor.Write(4, (ClipTimeLeft - new DateTime(1970, 1, 1)).TotalMilliseconds);
			//accessor.Write(12, (ClipTimeRight - new DateTime(1970, 1, 1)).TotalMilliseconds);

			accessor.Write(16, SelectedInputLeft);
			accessor.Write(20, SelectedInputRight);
			accessor.Write(24, SelectedOutputLeft);
			accessor.Write(28, SelectedOutputRight);

			accessor.Write(32, Gain);
			accessor.Write(36, IrLength);

			var idx = 40;

			for (int i = 0; i < IrLength; i++)
			{
				accessor.Write(idx, IrLeft[i]);
				idx += 4;
			}

			for (int i = 0; i < IrLength; i++)
			{
				accessor.Write(idx, IrRight[i]);
				idx += 4;
			}
		}

		public static SharedMemoryState Read(MemoryMappedViewAccessor accessor, int currentIndex)
		{
			var id = accessor.ReadInt32(0);
			if (id <= currentIndex)
				return null;

			//var clipTimeLeftMillis = accessor.ReadDouble(4);
			//var clipTimeRightMillis = accessor.ReadDouble(12);

			var selectedInputLeft = accessor.ReadInt32(16);
			var selectedInputRight = accessor.ReadInt32(20);
			var selectedOutputLeft = accessor.ReadInt32(24);
			var selectedOutputRight = accessor.ReadInt32(28);

			var gain = accessor.ReadSingle(32);
			var irLen = accessor.ReadInt32(36);

			var idx = 40;
			var irLeft = new float[irLen];
			var irRight = new float[irLen];

			for (int i = 0; i < irLen; i++)
			{
				irLeft[i] = accessor.ReadSingle(idx);
				idx += 4;
			}

			for (int i = 0; i < irLen; i++)
			{
				irRight[i] = accessor.ReadSingle(idx);
				idx += 4;
			}

			return new SharedMemoryState
			{
				Gain = gain,
				Id = id,
				IrLeft = irLeft,
				IrLength = irLen,
				IrRight = irRight,
				SelectedInputLeft = selectedInputLeft,
				SelectedInputRight = selectedInputRight,
				SelectedOutputLeft = selectedOutputLeft,
				SelectedOutputRight = selectedOutputRight
			};
		}

		public static void WriteClipIndicators(MemoryMappedViewAccessor accessor, DateTime left, DateTime right)
		{
			accessor.Write(4, (left - new DateTime(1970, 1, 1)).TotalMilliseconds);
			accessor.Write(12, (right - new DateTime(1970, 1, 1)).TotalMilliseconds);
		}

		public static DateTime[] ReadClipIndicators(MemoryMappedViewAccessor accessor)
		{
			var clipTimeLeftMillis = accessor.ReadDouble(4);
			var clipTimeRightMillis = accessor.ReadDouble(12);

			var clipTimeLeft = new DateTime(1970, 1, 1).AddMilliseconds(clipTimeLeftMillis);
			var clipTimeRight = new DateTime(1970, 1, 1).AddMilliseconds(clipTimeRightMillis);
			return new[] { clipTimeLeft, clipTimeRight };
		}

	}
}

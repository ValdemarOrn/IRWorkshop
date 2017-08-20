using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImpulseHd
{
	public unsafe class ProcessConv
	{
		public void Process()
		{
			
		}

		[DllImport("ImpulseHd.Convolution.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ProcessConv")]
		public static extern void Process(float* input, float* output, int len, float gain, int* bufferindex, float* ir, int irLen, float* buffer131k, int* clipped);

		public static void Process(float[] input, float[] output, float gain, ref int bufferIndex, float[] ir, float[] buffer, ref bool clipped)
		{
			fixed (float* inputPtr = input)
			fixed (float* outputPtr = output)
			fixed (float* irPtr = ir)
			fixed (float* bufferPtr = buffer)
			fixed (int* bufferIndexPtr = &bufferIndex)
			{
				int clippedInt = 0;
				Process(inputPtr, outputPtr, input.Length, gain, bufferIndexPtr, irPtr, ir.Length, bufferPtr, &clippedInt);
				clipped = clippedInt > 0;
			}
		}
	}
}

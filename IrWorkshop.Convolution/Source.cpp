
//using namespace std;

extern "C"
{
	const int bufferLen = 131072;

	_declspec(dllexport) void ProcessConv(float* input, float* output, int len, float gain, int* bufferindex, float* ir, int irLen, float* buffer131k, int* clipped)
	{
		const int sizeMask = bufferLen - 1;
		const int ix = *bufferindex;
		*clipped = 0;

		for (int i = 0; i < len; i++)
		{
			int readPos = (ix + i) & sizeMask;
			float sample = input[i];

			for (int j = 0; j < irLen; j++)
			{
				int writePos = (readPos + j) & sizeMask;
				buffer131k[writePos] += sample * ir[j];
			}

			float outputSample = buffer131k[readPos] * gain;
			buffer131k[readPos] = 0.0f;

			if (outputSample < -0.98f)
			{
				outputSample = -0.98f;
				*clipped = 1;
			}
			if (outputSample > 0.98f)
			{
				outputSample = 0.98f;
				*clipped = 1;
			}
			output[i] = outputSample;
		}

		*bufferindex += len;
		if (*bufferindex >= bufferLen)
			*bufferindex -= bufferLen;
	}
}

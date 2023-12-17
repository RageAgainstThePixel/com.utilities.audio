// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.Audio
{
    public static class PCMEncoder
    {
        /// <summary>
        /// Encodes the <see cref="samples"/> to raw pcm bytes.
        /// </summary>
        /// <param name="samples">Raw sample data</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="trim">Optional, trim the silence from the data.</param>
        /// <param name="silenceThreshold">Optional, silence threshold to use for trimming operations.</param>
        /// <returns>Byte array PCM data.</returns>
        public static byte[] Encode(float[] samples, PCMFormatSize size = PCMFormatSize.EightBit, bool trim = false, float silenceThreshold = 1.0e-5f)
        {
            var sampleCount = samples.Length;

            // trim data
            var start = 0;
            var end = sampleCount;
            var length = sampleCount;

            if (trim)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    // Replace the condition for detecting non-silence
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        start = i;
                        break;
                    }
                }

                for (var i = sampleCount - 1; i >= 0; i--)
                {
                    // Replace the condition for detecting non-silence
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        end = i + 1;
                        break;
                    }
                }

                length = end - start;

                if (length < 1)
                {
                    throw new InvalidOperationException("Trimming operation failed!");
                }
            }

            var offset = (int)size;
            var pcmData = new byte[length * offset];

            // convert and write data
            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = start; i < end; i++)
                    {
                        var sample = Math.Min(Math.Max((samples[i] * 127.5f) + 128, byte.MinValue), byte.MaxValue);
                        pcmData[i - start] = (byte)sample;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = start; i < end; i++)
                    {
                        var sample = (short)Math.Min(Math.Max(samples[i] * short.MaxValue, short.MinValue), short.MaxValue);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)sample;
                        pcmData[stride + 1] = (byte)(sample >> 8);
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = start; i < end; i++)
                    {
                        const int max24Value = 8388607;
                        const int min24Value = -8388608;
                        var scaledSample = samples[i] * max24Value; // 2^23 - 1
                        var sample = (int)scaledSample;
                        var stride = (i - start) * offset;
                        // Clamping to a valid 24-bit range prior to conversion
                        sample = Math.Min(Math.Max(sample, min24Value), max24Value);
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                        pcmData[stride + 2] = (byte)((sample >> 16) & byte.MaxValue);
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = start; i < end; i++)
                    {
                        var scale = (samples[i] >= 0) ? int.MaxValue : int.MinValue;
                        var sample = (int)(samples[i] * scale);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)sample;
                        pcmData[stride + 1] = (byte)(sample >> 8);
                        pcmData[stride + 2] = (byte)(sample >> 16);
                        pcmData[stride + 3] = (byte)(sample >> 24);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }

            return pcmData;
        }

        /// <summary>
        /// Decodes the raw PCM byte data to samples.<br/>
        /// </summary>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        public static float[] Decode(byte[] pcmData, PCMFormatSize size = PCMFormatSize.EightBit)
        {
            if (pcmData.Length % (int)size != 0)
            {
                throw new ArgumentException($"{nameof(pcmData)} length must be multiple of the specified {nameof(PCMFormatSize)}!", nameof(pcmData));
            }

            var sampleCount = pcmData.Length / (int)size;
            var samples = new float[sampleCount];
            var sampleIndex = 0;

            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        samples[sampleIndex++] = (pcmData[i] - 128f) / 128f;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = BitConverter.ToInt16(pcmData, i * sizeof(short));
                        samples[sampleIndex++] = sample / (float)short.MaxValue;
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = (pcmData[i * 3] << 8) | (pcmData[i * 3 + 1] << 16) | (pcmData[i * 3 + 2] << 24);
                        samples[sampleIndex++] = sample / 8388607f;
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = BitConverter.ToInt32(pcmData, i * sizeof(int));
                        samples[sampleIndex++] = sample / (float)int.MaxValue;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }

            return samples;
        }

        /// <summary>
        /// Resample the sample data to the specified sampling rate.
        /// </summary>
        /// <param name="samples">Samples to resample.</param>
        /// <param name="inputSamplingRate">The sampling rate of the samples provided.</param>
        /// <param name="outputSamplingRate">The target sampling rate to resample to.</param>
        /// <returns>Float array of samples at specified output sampling rate.</returns>
        public static float[] Resample(float[] samples, int inputSamplingRate, int outputSamplingRate)
        {
            var ratio = (double)outputSamplingRate / inputSamplingRate;
            var outputLength = (int)(samples.Length * ratio);
            var result = new float[outputLength];

            for (var i = 0; i < outputLength; i++)
            {
                var position = i / ratio;
                var leftIndex = (int)Math.Floor(position);
                var rightIndex = leftIndex + 1;
                var fraction = position - leftIndex;

                if (rightIndex >= samples.Length)
                {
                    result[i] = samples[leftIndex];
                }
                else
                {
                    result[i] = (float)(samples[leftIndex] * (1 - fraction) + samples[rightIndex] * fraction);
                }
            }

            return result;
        }
    }
}

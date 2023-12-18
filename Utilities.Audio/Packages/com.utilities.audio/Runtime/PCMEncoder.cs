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
        public static byte[] Encode(float[] samples, PCMFormatSize size = PCMFormatSize.EightBit, bool trim = false, float silenceThreshold = 0.001f)
        {
            var sampleCount = samples.Length;
            var start = 0;
            var end = sampleCount;
            var length = sampleCount;

            if (trim)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        start = Math.Max(i - 1, 0);
                        break;
                    }
                }

                for (var i = sampleCount - 1; i >= start; i--)
                {
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        end = i + 1;
                        break;
                    }
                }

                length = end - start;

                if (length <= 0)
                {
                    throw new InvalidOperationException("Trimming operation failed due to incorrect silence detection.");
                }
            }

            var offset = (int)size;
            var pcmData = new byte[length * offset];

            // Ensuring samples are within [-1,1] range
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = Math.Max(-1f, Math.Min(1f, samples[i]));
            }

            // Convert and write data
            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)Math.Max(Math.Min(Math.Round(value * 127 + 128), 255), 0);
                        pcmData[i - start] = (byte)sample;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (short)(value * short.MaxValue);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)(value * ((1 << 23) - 1));
                        sample = Math.Min(Math.Max(sample, -(1 << 23)), (1 << 23) - 1);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                        pcmData[stride + 2] = (byte)((sample >> 16) & byte.MaxValue);
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)(value * int.MaxValue);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                        pcmData[stride + 2] = (byte)((sample >> 16) & byte.MaxValue);
                        pcmData[stride + 3] = (byte)((sample >> 24) & byte.MaxValue);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }

            return pcmData;
        }

        /// <summary>
        /// Decodes the raw PCM byte data to samples.
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
                        var sample = pcmData[i];
                        var normalized = (sample - 128f) / 127f; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = (short)((pcmData[i * 2 + 1] << 8) | pcmData[i * 2]);
                        var normalized = sample / (float)short.MaxValue; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = (pcmData[i * 3] << 0) | (pcmData[i * 3 + 1] << 8) | (pcmData[i * 3 + 2] << 16);
                        sample = (sample & 0x800000) != 0 ? sample | unchecked((int)0xff000000) : sample & 0x00ffffff;
                        var normalized = sample / (float)(1 << 23); // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = 0; i < pcmData.Length; i += 4)
                    {
                        var sample = (pcmData[i + 3] << 24) | (pcmData[i + 2] << 16) | (pcmData[i + 1] << 8) | pcmData[i];
                        var normalized = sample / (float)int.MaxValue; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
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

// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine.Assertions;

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
        /// <returns>Byte array PCM data.</returns>
        public static byte[] Encode(float[] samples, PCMFormatSize size = PCMFormatSize.EightBit, bool trim = false)
        {
            var sampleCount = samples.Length;

            // trim data
            var start = 0;
            var end = sampleCount;

            if (trim)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    if (samples[i] * short.MaxValue == 0)
                    {
                        continue;
                    }

                    start = i;
                    break;
                }

                for (var i = sampleCount - 1; i >= 0; i--)
                {
                    if (samples[i] * short.MaxValue == 0)
                    {
                        continue;
                    }

                    end = i + 1;
                    break;
                }
            }

            var trimmedLength = end - start;
            Assert.IsTrue(trimmedLength > 0);
            Assert.IsTrue(trimmedLength <= sampleCount);
            var sampleIndex = 0;
            var pcmData = size switch
            {
                PCMFormatSize.EightBit => new byte[trimmedLength * sizeof(short)],
                PCMFormatSize.SixteenBit => new byte[trimmedLength * sizeof(float)],
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
            };

            // convert and write data
            for (var i = start; i < end; i++)
            {
                var sample = (short)(samples[i] * short.MaxValue);
                pcmData[sampleIndex++] = (byte)(sample >> 0);
                pcmData[sampleIndex++] = (byte)(sample >> 8);
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
            var sampleCount = pcmData.Length / (sizeof(short) * (int)size);
            var samples = new float[sampleCount];
            var sampleIndex = 0;

            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = 0; i < pcmData.Length; i++)
                    {
                        samples[sampleIndex++] = pcmData[i] / 128f - 1f;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = 0; i < pcmData.Length; i += 2)
                    {
                        var sample = (short)((pcmData[i + 1] << 8) | pcmData[i]);
                        samples[sampleIndex++] = sample / (float)short.MaxValue;
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

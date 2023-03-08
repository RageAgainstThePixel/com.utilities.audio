// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Utilities.Audio
{
    public static class AudioClipExtensions
    {
        /// <summary>
        /// Encodes the <see cref="AudioClip"/> to PCM.<br/>
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="trim">Optional, trim the silence from the data.</param>
        /// <returns>Byte array PCM data.</returns>
        public static byte[] EncodeToPCM(this AudioClip audioClip, PCMFormatSize size = PCMFormatSize.EightBit, bool trim = false)
        {
            var samples = new float[audioClip.samples * audioClip.channels];
            var sampleCount = samples.Length;
            audioClip.GetData(samples, 0);

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
    }
}

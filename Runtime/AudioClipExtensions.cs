// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.Assertions;

namespace Utilities.Audio
{
    public static class AudioClipExtensions
    {
        public static byte[] EncodeToPCM(this AudioClip audioClip, bool trim = false)
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
                    if (samples[i] * Constants.RescaleFactor == 0)
                    {
                        continue;
                    }

                    start = i;
                    break;
                }

                for (var i = sampleCount - 1; i >= 0; i--)
                {
                    if (samples[i] * Constants.RescaleFactor == 0)
                    {
                        continue;
                    }

                    end = i + 1;
                    break;
                }
            }

            var trimmedLength = end - start;
            Assert.IsTrue(trimmedLength > 0);
            var sampleIndex = 0;
            var pcmData = new byte[trimmedLength * sizeof(float)];

            // convert and write data
            for (var i = start; i < end; i++)
            {
                var sample = (short)(samples[i] * Constants.RescaleFactor);
                pcmData[sampleIndex++] = (byte)(sample >> 0);
                pcmData[sampleIndex++] = (byte)(sample >> 8);
            }

            return pcmData;
        }
    }
}

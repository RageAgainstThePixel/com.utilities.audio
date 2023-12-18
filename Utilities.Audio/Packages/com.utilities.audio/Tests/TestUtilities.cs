// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.Audio.Tests
{
    internal static class TestUtilities
    {
        public static float[] GenerateSineWaveSamples(double frequency, int sampleRate, float duration = 1f, float amplitude = 0.5f)
        {
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var period = i / (double)sampleRate;
                var value = amplitude * Math.Sin(2 * Math.PI * frequency * period);
                samples[i] = (float)value;
            }

            NormalizeSamples(ref samples);
            return samples;
        }

        public static float[] GenerateSineWaveSamplesWithSilence(double frequency, int sampleRate, int silenceSamplesAtStart, int silenceSamplesAtEnd, float duration = 1f, float amplitude = 0.5f)
        {
            // Assuming sampleRate is the number of non-silent samples excluding any silence
            var sampleCount = (int)(sampleRate * duration) + silenceSamplesAtStart + silenceSamplesAtEnd;
            var samples = new float[sampleCount];

            // Generate silence at the start of the sample
            for (var i = 0; i < silenceSamplesAtStart; i++)
            {
                samples[i] = 0.0f;
            }

            // The actual sine wave
            for (var i = silenceSamplesAtStart; i < sampleRate + silenceSamplesAtStart; i++)
            {
                var period = (i - silenceSamplesAtStart) / (double)sampleRate;
                var value = amplitude * Math.Sin(2 * Math.PI * frequency * period);
                samples[i] = (float)value;
            }

            // Generate silence at the end of the sample
            for (var i = sampleRate + silenceSamplesAtStart; i < sampleCount; i++)
            {
                samples[i] = 0.0f;
            }

            NormalizeSamples(ref samples);
            return samples;
        }

        private static void NormalizeSamples(ref float[] samples)
        {
            var maxAbsValue = 0f;

            // Find the maximum absolute value in the samples
            foreach (var sample in samples)
            {
                var absValue = Math.Abs(sample);
                if (absValue > maxAbsValue)
                {
                    maxAbsValue = absValue;
                }
            }

            // Normalize the samples based on the maximum absolute value
            if (maxAbsValue > 1.0f)
            {
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] /= maxAbsValue;
                }
            }
        }
    }
}

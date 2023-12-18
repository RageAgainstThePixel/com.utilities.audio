// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.Audio.Tests
{
    internal static class TestUtilities
    {
        public static float[] GenerateSineWaveSamples(double frequency, int sampleRate)
        {
            var samples = new float[sampleRate]; // Only create samples for the sine wave
            var samplePeriod = 1.0 / sampleRate;

            // Generate the actual sine wave
            for (var i = 0; i < sampleRate; i++)
            {
                var time = i * samplePeriod;
                var sineValue = Math.Sin(2 * Math.PI * frequency * time);
                samples[i] = (float)sineValue;
            }

            return samples;
        }

        public static float[] GenerateSineWaveSamplesWithSilence(double frequency, int sampleRate, int silenceSamplesAtStart, int silenceSamplesAtEnd)
        {    // Assuming sampleRate is the number of non-silent samples excluding any silence
            var totalSampleCount = sampleRate + silenceSamplesAtStart + silenceSamplesAtEnd;
            var samples = new float[totalSampleCount];
            // Total samples including non-silent and silent samples
            var samplePeriod = 1.0 / sampleRate;

            // Generate silence at the start of the sample
            for (int i = 0; i < silenceSamplesAtStart; i++)
            {
                samples[i] = 0.0f;
            }

            // The actual sine wave
            for (int i = silenceSamplesAtStart; i < sampleRate + silenceSamplesAtStart; i++)
            {
                var time = (i - silenceSamplesAtStart) * samplePeriod;
                samples[i] = (float)Math.Sin(2 * Math.PI * frequency * time);
            }

            // Generate silence at the end of the sample
            for (int i = sampleRate + silenceSamplesAtStart; i < totalSampleCount; i++)
            {
                samples[i] = 0.0f;
            }

            return samples;
        }
    }
}

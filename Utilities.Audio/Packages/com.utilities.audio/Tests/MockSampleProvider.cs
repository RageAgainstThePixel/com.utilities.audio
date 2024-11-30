// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using UnityEngine;

namespace Utilities.Audio.Tests
{
    internal class MockSampleProvider : ISampleProvider
    {
        private readonly float[] inputBuffer;
        private readonly float[] fullSamples;
        private int micPosition;
        private int sampleRate;
        private int totalTime;

        public MockSampleProvider(float[] fullSamples, int sampleRate)
        {
            // buffer will always be one second long.
            this.inputBuffer = new float[sampleRate];
            this.sampleRate = sampleRate;
            this.fullSamples = fullSamples;
            micPosition = 0;
            totalTime = 0;
        }

        public int GetPosition(string deviceName)
        {
            // unity will move this ahead depending on frame rate.
            // for now just move it ahead one second.
            micPosition += sampleRate;
            totalTime += micPosition;

            // loop back to the beginning if we've reached the end
            if (micPosition >= fullSamples.Length)
            {
                micPosition = 0;
            }

            return micPosition;
        }

        public void GetData(AudioClip clip, float[] buffer)
        {
            // Calculate the number of samples to copy
            var samplesToCopy = Math.Min(buffer.Length, fullSamples.Length);

            // Copy the samples from the fullSamples array to the buffer
            Array.Copy(fullSamples, 0, buffer, 0, samplesToCopy);

            // If the buffer is larger than the remaining samples, wrap around and copy from the beginning
            if (samplesToCopy < buffer.Length)
            {
                Array.Copy(fullSamples, 0, buffer, samplesToCopy, buffer.Length - samplesToCopy);
            }
        }

        public void End(string deviceName)
        {
            // No-op for mock
        }
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using UnityEngine;

namespace Utilities.Audio.Tests
{
    internal class MockSampleProvider : ISampleProvider
    {
        private int micPosition;
        private int maxSamples;
        private int totalSamples;
        private int sampleRate;
        private float[] recordedSamples;

        public MockSampleProvider(float[] recordedSamples, int sampleRate, int time)
        {
            micPosition = 0;
            this.sampleRate = sampleRate;
            maxSamples = time * sampleRate;
            this.recordedSamples = recordedSamples;
        }

        public int GetPosition(string deviceName)
        {
            if (totalSamples >= maxSamples)
            {
                Debug.LogError($"max samples hit {totalSamples} >= {maxSamples}");
                return micPosition;
            }

            micPosition += sampleRate / 10;

            if (micPosition >= sampleRate)
            {
                micPosition -= sampleRate;
                totalSamples += micPosition;
                Debug.LogWarning($"loopback {micPosition} | {totalSamples}");
            }
            else
            {
                totalSamples += micPosition;
                Debug.Log($"pos: {micPosition} | {totalSamples}");
            }

            return micPosition;
        }

        public void GetData(AudioClip clip, float[] buffer)
        {
            Assert.AreEqual(buffer.Length, sampleRate);
            Assert.GreaterOrEqual(micPosition, 0, "micPosition must be non-negative.");
            Array.Copy(recordedSamples, micPosition, buffer, 0, buffer.Length);
        }

        public void End(string deviceName)
        {
            micPosition = -1;
        }
    }
}

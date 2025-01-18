// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace Utilities.Audio.Tests
{
    internal class TestFixture_01_PCM_Encoding
    {
        private const float Tolerance = 0.01f;
        private const double TestFrequency = 440; // A4 note.
        private const int k_96000 = 96000;
        private const int k_48000 = 48000;
        private const int k_44100 = 44100; // Default unity sample rate.
        private const int k_22050 = 22050;
        private const int k_24000 = 24000;
        private const int k_16000 = 16000;

        [OneTimeSetUp]
        public void Setup()
        {
            if (!Directory.Exists("test-samples"))
            {
                Directory.CreateDirectory("test-samples");
            }
        }

        [Test]
        public void Test_00_01_SinWaveGeneration()
        {
            const int fixedSilenceAtStart = 512;
            const int fixedSilenceAtEnd = 256;
            var sineWaveSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            Assert.NotNull(sineWaveSamples);
            Assert.AreEqual(k_44100, sineWaveSamples.Length);
            var encodedSineWave = PCMEncoder.Encode(sineWaveSamples, PCMFormatSize.EightBit);
            Assert.NotNull(encodedSineWave);
            Assert.AreEqual(k_44100 * (int)PCMFormatSize.EightBit, encodedSineWave.Length);
            var sineWaveWithSilenceSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, k_44100, fixedSilenceAtStart, fixedSilenceAtEnd);
            Assert.NotNull(sineWaveWithSilenceSamples);
            Assert.AreEqual(k_44100 + fixedSilenceAtStart + fixedSilenceAtEnd, sineWaveWithSilenceSamples.Length);
            var encodedSineWaveWithSilence = PCMEncoder.Encode(sineWaveWithSilenceSamples, PCMFormatSize.EightBit, true);
            Assert.NotNull(encodedSineWaveWithSilence);
            Assert.AreEqual(k_44100 * (int)PCMFormatSize.EightBit, encodedSineWaveWithSilence.Length);

            var sampleCount = encodedSineWave.Length;

            for (var i = 0; i < sampleCount; i++)
            {
                Assert.IsTrue(Math.Abs(encodedSineWave[i] - encodedSineWaveWithSilence[i]) < 0.001f, $"Sample at index [{i}] {encodedSineWaveWithSilence[i]} does not match expected value: {encodedSineWave[i]}!");
            }
        }

        [Test]
        public void Test_00_02_Resample_SameSampleRate_ReturnsOriginalSamples()
        {
            float[] samples = { 0.1f, 0.2f, 0.3f };
            var result = PCMEncoder.Resample(samples, null, k_44100, k_44100);
            Assert.AreEqual(samples, result);
        }

        [Test]
        public void Test_00_03_Resample_DifferentSampleRate_ResamplesCorrectly()
        {    // Test case 1
            float[] samples1 = { 0.1f, 0.2f, 0.3f, 0.4f };

            var result1 = PCMEncoder.Resample(samples1, null, k_44100, k_24000);
            Debug.Log($"values: {string.Join(',', result1)}");
            Assert.AreEqual(2, result1.Length);
            Assert.AreEqual(0.1f, result1[0], Tolerance);
            Assert.AreEqual(0.3f, result1[1], Tolerance);

            // assert that each value is greater than or equal to the prev
            for (var i = 1; i < result1.Length; i++)
            {
                Assert.GreaterOrEqual(result1[i], result1[i - 1]);
            }

            // Test case 2
            float[] samples2 = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f };

            var result2 = PCMEncoder.Resample(samples2, null, k_48000, k_16000);

            Assert.AreEqual(3, result2.Length);
            Assert.AreEqual(0.1f, result2[0], Tolerance);
            Assert.AreEqual(0.4f, result2[1], Tolerance);
            Assert.AreEqual(0.7f, result2[2], Tolerance);

            // assert that each value is greater than or equal to the prev
            for (var i = 1; i < result2.Length; i++)
            {
                Assert.GreaterOrEqual(result2[i], result2[i - 1]);
            }

            // Test case 3
            float[] samples3 = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };

            var result3 = PCMEncoder.Resample(samples3, null, k_44100, k_22050);

            Assert.AreEqual(5, result3.Length);
            Assert.AreEqual(0.1f, result3[0], Tolerance);
            Assert.AreEqual(0.3f, result3[1], Tolerance);
            Assert.AreEqual(0.5f, result3[2], Tolerance);
            Assert.AreEqual(0.7f, result3[3], Tolerance);
            Assert.AreEqual(0.9f, result3[4], Tolerance);

            // assert that each value is greater than or equal to the prev
            for (var i = 1; i < result3.Length; i++)
            {
                Assert.GreaterOrEqual(result3[i], result3[i - 1]);
            }

            // Test case 4
            float[] samples4 = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };

            var result4 = PCMEncoder.Resample(samples4, null, k_16000, k_96000);

            Assert.AreEqual(60, result4.Length);
            Assert.AreEqual(0.1f, result4[0], Tolerance);
            Assert.AreEqual(0.3f, result4[10], Tolerance);
            Assert.AreEqual(0.4f, result4[20], Tolerance);
            Assert.AreEqual(0.6f, result4[30], Tolerance);
            Assert.AreEqual(0.8f, result4[40], Tolerance);
            Assert.AreEqual(0.9f, result4[50], Tolerance);
            Assert.AreEqual(1f, result4[59], Tolerance);

            // assert that each value is greater than or equal to the prev
            for (var i = 1; i < result4.Length; i++)
            {
                Assert.GreaterOrEqual(result4[i], result4[i - 1], $"Expected [{i}] {result4[i]} >= [{i - 1}] {result4[i - 1]}");
            }
        }

        [Test]
        public void Test_00_04_Resample_WithBuffer_UsesProvidedBuffer()
        {
            float[] samples = { 0.1f, 0.2f, 0.3f, 0.4f };
            var buffer = new float[2];

            var result = PCMEncoder.Resample(samples, buffer, k_44100, k_24000);

            Assert.AreEqual(buffer, result);
            Assert.AreEqual(0.1f, buffer[0], Tolerance);
            Assert.AreEqual(0.3f, buffer[1], Tolerance);
        }

        [Test]
        public void Test_00_05_Resample_EmptySamples_ReturnsEmptyBuffer()
        {
            float[] samples = { };

            var result = PCMEncoder.Resample(samples, null, k_44100, k_24000);

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Test_00_06_Resample_SingleSample_ReturnsSingleSample()
        {
            float[] samples = { 0.1f };

            var result = PCMEncoder.Resample(samples, null, k_44100, k_24000);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0.1f, result[0], Tolerance);
        }

        [Test]
        public void Test_00_07_01_Resample_LargerSampleArray_ResamplesCorrectly()
        {
            var samples = new float[k_44100];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = (float)i / samples.Length;
            }

            var result = PCMEncoder.Resample(samples, null, k_44100, k_24000);

            Assert.AreEqual(24000, result.Length);
            Assert.AreEqual(0f, result[0], Tolerance);
            Assert.AreEqual(0.25f, result[6000], Tolerance);
            Assert.AreEqual(0.5f, result[12000], Tolerance);
            Assert.AreEqual(0.75f, result[18000], Tolerance);
            Assert.AreEqual(1f, result[23999], Tolerance);
        }

        [Test]
        public void Test_00_07_02_Resample_SmallerSampleArray_ResamplesCorrectly()
        {
            var samples = new float[k_16000];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = (float)i / samples.Length;
            }

            var result = PCMEncoder.Resample(samples, null, k_16000, k_96000);

            Assert.AreEqual(k_96000, result.Length);
            Assert.AreEqual(0f, result[0], Tolerance);
            Assert.AreEqual(0.25f, result[24000], Tolerance);
            Assert.AreEqual(0.5f, result[48000], Tolerance);
            Assert.AreEqual(0.75f, result[72000], Tolerance);
            Assert.AreEqual(1f, result[95999], Tolerance);
        }

        [Test]
        public void Test_00_08_Resample_SineWav_ResamplesCorrectly()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            for (var i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)i / testSamples.Length;
            }

            var downSample = PCMEncoder.Resample(testSamples, null, k_44100, k_24000);

            Assert.AreEqual(24000, downSample.Length);
            Assert.AreEqual(0f, downSample[0], Tolerance);
            Assert.AreEqual(0.25f, downSample[6000], Tolerance);
            Assert.AreEqual(0.5f, downSample[12000], Tolerance);
            Assert.AreEqual(0.75f, downSample[18000], Tolerance);
            Assert.AreEqual(1f, downSample[23999], Tolerance);

            var upSample = PCMEncoder.Resample(downSample, null, k_24000, k_44100);

            Assert.AreEqual(44100, upSample.Length);
            Assert.AreEqual(0f, upSample[0], Tolerance);
            Assert.AreEqual(0.25f, upSample[11025], Tolerance);
            Assert.AreEqual(0.5f, upSample[22050], Tolerance);
            Assert.AreEqual(0.75f, upSample[33075], Tolerance);
            Assert.AreEqual(1f, upSample[44099], Tolerance);
        }

        [Test]
        public void Test_01_01_PCM_Encode_8Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.EightBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.EightBit);

            File.WriteAllBytes("test-samples/8bit-sine.pcm", encodedBytes);

            // Assert at the end of the unit test
            Assert.AreEqual(k_44100, decodedSamples.Length);

            const float quantizationTolerance = 1.0f / 127; // Tolerance for 8-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_01_02_PCM_Encode_8Bit_TrimSilence()
        {
            const int fixedSilenceAtStart = 512;
            const int fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, k_44100, fixedSilenceAtStart, fixedSilenceAtEnd);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.EightBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.EightBit);

            File.WriteAllBytes("test-samples/8bit-sine-trimmed.pcm", encodedBytes);

            // The decoded samples should be only the non-silent portion of the original samples.
            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            const float k_8bit_quantizationTolerance = 1.0f / 127; // Tolerance for 8-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                // Compare each non-silent decoded sample against the generated sine wave samples, allowing for quantization error.
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], k_8bit_quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_01_03_PCM_Encode_8Bit_Resampled()
        {
            var originalSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var resampledSamples = PCMEncoder.Resample(originalSamples, null, k_44100, k_24000);

            var encodedBytes = PCMEncoder.Encode(resampledSamples, PCMFormatSize.EightBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.EightBit);

            File.WriteAllBytes("test-samples/8bit-sine-resampled.pcm", encodedBytes);

            Assert.AreEqual(k_24000, decodedSamples.Length, "Decoded samples length should match the resampled sample length.");

            const float quantizationTolerance = 1.0f / 127; // Tolerance for 8-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(resampledSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_02_01_PCM_Encode_16Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.SixteenBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.SixteenBit);

            File.WriteAllBytes("test-samples/16bit-sine.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            const float quantizationTolerance = 1.0f / short.MaxValue; // Tolerance for 16-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_02_02_PCM_Encode_16Bit_TrimSilence()
        {
            // Adjust silence for 16-bit representation if needed
            const int fixedSilenceAtStart = 512;
            const int fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, k_44100, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.SixteenBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.SixteenBit);

            File.WriteAllBytes("test-samples/16bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            const float quantizationTolerance = 1.0f / short.MaxValue; // Tolerance for 16-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_02_03_PCM_Encode_16Bit_Resampled()
        {
            var originalSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_48000);
            var resampledSamples = PCMEncoder.Resample(originalSamples, null, k_48000, k_44100);

            var encodedBytes = PCMEncoder.Encode(resampledSamples, PCMFormatSize.SixteenBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.SixteenBit);

            File.WriteAllBytes("test-samples/16bit-sine-resampled.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the resampled sample length.");

            const float quantizationTolerance = 1.0f / short.MaxValue; // Tolerance for 16-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(resampledSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_03_01_PCM_Encode_24Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.TwentyFourBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.TwentyFourBit);

            File.WriteAllBytes("test-samples/24bit-sine.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            const float quantizationTolerance = 2.0f / (1 << 23); // Tolerance for 24-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_03_02_PCM_Encode_24Bit_TrimSilence()
        {
            // Adjust silence for 24-bit representation if needed
            const int fixedSilenceAtStart = 512;
            const int fixedSilenceAtEnd = 256;

            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, k_44100, fixedSilenceAtStart, fixedSilenceAtEnd);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.TwentyFourBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.TwentyFourBit);

            File.WriteAllBytes("test-samples/24bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            const float quantizationTolerance = 2.0f / (1 << 23); // Tolerance for 24-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_03_03_PCM_Encode_24Bit_Resampled()
        {
            var originalSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var resampledSamples = PCMEncoder.Resample(originalSamples, null, k_44100, k_24000);

            var encodedBytes = PCMEncoder.Encode(resampledSamples, PCMFormatSize.TwentyFourBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.TwentyFourBit);

            File.WriteAllBytes("test-samples/24bit-sine-resampled.pcm", encodedBytes);

            Assert.AreEqual(k_24000, decodedSamples.Length, "Decoded samples length should match the resampled sample length.");

            const float quantizationTolerance = 2.0f / (1 << 23); // Tolerance for 24-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(resampledSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_04_01_PCM_Encode_32Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.ThirtyTwoBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.ThirtyTwoBit);

            File.WriteAllBytes("test-samples/32bit-sine.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            const float quantizationTolerance = 1.0f / int.MaxValue; // Tolerance for 32-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_04_02_PCM_Encode_32Bit_TrimSilence()
        {
            // Adjust silence for 32-bit representation if needed
            const int fixedSilenceAtStart = 512;
            const int fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, k_44100, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.ThirtyTwoBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.ThirtyTwoBit);

            File.WriteAllBytes("test-samples/32bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(k_44100, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            const float quantizationTolerance = 1.0f / int.MaxValue; // Tolerance for 32-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_04_03_PCM_Encode_24Bit_Resampled()
        {
            var originalSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, k_44100);
            var resampledSamples = PCMEncoder.Resample(originalSamples, null, k_44100, k_24000);

            var encodedBytes = PCMEncoder.Encode(resampledSamples, PCMFormatSize.ThirtyTwoBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.ThirtyTwoBit);

            File.WriteAllBytes("test-samples/32bit-sine-resampled.pcm", encodedBytes);

            Assert.AreEqual(k_24000, decodedSamples.Length, "Decoded samples length should match the resampled sample length.");

            const float quantizationTolerance = 1.0f / int.MaxValue; // Tolerance for 32-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(resampledSamples[i], decodedSamples[i], quantizationTolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.IO;

namespace Utilities.Audio.Tests
{
    internal class TestFixture_01_PCM_Encoding
    {
        private const double TestFrequency = 440;  // A4 note.
        private const int TestSampleRate = 44100;  // Default unity sample rate.

        [OneTimeSetUp]
        public void Setup()
        {
            if (!Directory.Exists("test-samples"))
            {
                Directory.CreateDirectory("test-samples");
            }
        }

        [Test]
        public void Test_00_SinWaveGeneration()
        {
            var fixedSilenceAtStart = 512;
            var fixedSilenceAtEnd = 256;
            var sineWaveSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, TestSampleRate);
            var encodedSineWave = PCMEncoder.Encode(sineWaveSamples, PCMFormatSize.EightBit);
            var sineWaveWithSilenceSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, TestSampleRate, fixedSilenceAtStart, fixedSilenceAtEnd);
            var encodedSineWaveWithSilence = PCMEncoder.Encode(sineWaveWithSilenceSamples, PCMFormatSize.EightBit, true);
            var sampleCount = encodedSineWave.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                Assert.IsTrue(Math.Abs(encodedSineWave[i] - encodedSineWaveWithSilence[i]) < 0.001f, $"Sample at index [{i}] {encodedSineWaveWithSilence[i]} does not match expected value: {encodedSineWave[i]}!");
            }
        }

        [Test]
        public void Test_01_01_PCM_Encode_8Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, TestSampleRate);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.EightBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.EightBit);

            File.WriteAllBytes("test-samples/8bit-sine.pcm", encodedBytes);

            // Assert at the end of the unit test
            Assert.AreEqual(TestSampleRate, decodedSamples.Length);

            var tolerance = 1.0f / 127; // Tolerance for 8-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_01_02_PCM_Encode_8Bit_TrimSilence()
        {
            var fixedSilenceAtStart = 512;
            var fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, TestSampleRate, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.EightBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.EightBit);

            File.WriteAllBytes("test-samples/8bit-sine-trimmed.pcm", encodedBytes);

            // The decoded samples should be only the non-silent portion of the original samples.
            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            var tolerance = 1.0f / 127;  // Tolerance for 8-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                // Compare each non-silent decoded sample against the generated sine wave samples, allowing for quantization error.
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }
        [Test]
        public void Test_02_01_PCM_Encode_16Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, TestSampleRate);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.SixteenBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.SixteenBit);

            File.WriteAllBytes("test-samples/16bit-sine.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            var tolerance = 1.0f / short.MaxValue; // Tolerance for 16-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_02_02_PCM_Encode_16Bit_TrimSilence()
        {
            // Adjust silence for 16-bit representation if needed
            var fixedSilenceAtStart = 512;
            var fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, TestSampleRate, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.SixteenBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.SixteenBit);

            File.WriteAllBytes("test-samples/16bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            var tolerance = 1.0f / short.MaxValue; // Tolerance for 16-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_03_01_PCM_Encode_24Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, TestSampleRate);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.TwentyFourBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.TwentyFourBit);

            File.WriteAllBytes("test-samples/24bit-sine.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            var tolerance = 2.0f / (1 << 23);

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_03_02_PCM_Encode_24Bit_TrimSilence()
        {
            // Adjust silence for 24-bit representation if needed
            var fixedSilenceAtStart = 512;
            var fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, TestSampleRate, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.TwentyFourBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.TwentyFourBit);

            File.WriteAllBytes("test-samples/24bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            var tolerance = 2.0f / (1 << 23);

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_04_01_PCM_Encode_32Bit_NoTrim()
        {
            var testSamples = TestUtilities.GenerateSineWaveSamples(TestFrequency, TestSampleRate);
            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.ThirtyTwoBit);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.ThirtyTwoBit);

            File.WriteAllBytes("test-samples/32bit-sine.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the original sample length.");

            var tolerance = 1.0f / int.MaxValue; // Tolerance for 32-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }

        [Test]
        public void Test_04_02_PCM_Encode_32Bit_TrimSilence()
        {
            // Adjust silence for 32-bit representation if needed
            var fixedSilenceAtStart = 512;
            var fixedSilenceAtEnd = 256;
            var testSamples = TestUtilities.GenerateSineWaveSamplesWithSilence(TestFrequency, TestSampleRate, fixedSilenceAtStart, fixedSilenceAtEnd);

            var encodedBytes = PCMEncoder.Encode(testSamples, PCMFormatSize.ThirtyTwoBit, true);
            var decodedSamples = PCMEncoder.Decode(encodedBytes, PCMFormatSize.ThirtyTwoBit);

            File.WriteAllBytes("test-samples/32bit-sine-trimmed.pcm", encodedBytes);

            Assert.AreEqual(TestSampleRate, decodedSamples.Length, "Decoded samples length should match the non-silent sample length after trimming.");

            var tolerance = 1.0f / int.MaxValue; // Tolerance for 32-bit quantization

            for (var i = 0; i < decodedSamples.Length; i++)
            {
                Assert.AreEqual(testSamples[fixedSilenceAtStart + i], decodedSamples[i], tolerance, $"Sample value at index {i} after decoding is outside the allowed tolerance.");
            }
        }
    }
}

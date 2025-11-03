// Licensed under the MIT License. See LICENSE in the project root for license information.

using Unity.Collections;
using UnityEngine;

namespace Utilities.Audio
{
    public static class AudioClipExtensions
    {
        /// <summary>
        /// Encodes the <see cref="AudioClip"/> to PCM.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="trim">Optional, trim the silence from the data.</param>
        /// <param name="outputSampleRate">The expected output sample rate of the audio clip.</param>
        /// <param name="allocator"></param>
        /// <returns>Byte array PCM data.</returns>
        public static NativeArray<byte> EncodeToPCM(this AudioClip audioClip, PCMFormatSize size = PCMFormatSize.SixteenBit, bool trim = false, int outputSampleRate = 44100, Allocator allocator = Allocator.Temp)
        {
#if UNITY_6000_0_OR_NEWER
            var samples = new NativeArray<float>(audioClip.samples * audioClip.channels, Allocator.Persistent);
#else
            var samples = new float[audioClip.samples * audioClip.channels];
#endif

            try
            {
                audioClip.GetData(samples, 0);

                if (audioClip.frequency != outputSampleRate)
                {
#if UNITY_6000_0_OR_NEWER
                    var resampled = PCMEncoder.Resample(samples, audioClip.frequency, outputSampleRate, Allocator.Persistent);
                    samples.Dispose();
                    samples = resampled;
#else
                    samples = PCMEncoder.Resample(samples, audioClip.frequency, outputSampleRate);
#endif
                }
#if !UNITY_6000_0_OR_NEWER
                var nativeSamples = new NativeArray<float>(samples, Allocator.Persistent);

                try
                {
                    return PCMEncoder.Encode(nativeSamples, size, trim, allocator: allocator);
                }
                finally
                {
                    nativeSamples.Dispose();
                }
#else
                return PCMEncoder.Encode(samples, size, trim, allocator: allocator);
#endif

            }
            // ReSharper disable once RedundantEmptyFinallyBlock
            finally
            {
#if UNITY_6000_0_OR_NEWER
                samples.Dispose();
#endif
            }
        }

        /// <summary>
        /// Decodes the raw PCM byte data and sets it to the <see cref="AudioClip"/>.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="inputSampleRate">The sample rate of the <see cref="pcmData"/> provided.</param>
#if UNITY_6000_0_OR_NEWER
        [System.Obsolete("Use DecodeFromPCM with NativeArray")]
#endif
        public static void DecodeFromPCM(this AudioClip audioClip, byte[] pcmData, PCMFormatSize size = PCMFormatSize.SixteenBit, int inputSampleRate = 44100)
            => audioClip.SetData(PCMEncoder.Decode(pcmData, size, inputSampleRate, 44100), 0);

#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Decodes the raw PCM byte data and sets it to the <see cref="AudioClip"/>.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="inputSampleRate">The sample rate of the <see cref="pcmData"/> provided.</param>
        public static void DecodeFromPCM(this AudioClip audioClip, NativeArray<byte> pcmData, PCMFormatSize size = PCMFormatSize.SixteenBit, int inputSampleRate = 44100)
            => audioClip.SetData(PCMEncoder.Decode(pcmData, size, inputSampleRate, 44100), 0);
#endif
    }
}

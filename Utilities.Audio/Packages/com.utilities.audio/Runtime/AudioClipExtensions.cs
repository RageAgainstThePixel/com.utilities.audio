// Licensed under the MIT License. See LICENSE in the project root for license information.

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
        /// <returns>Byte array PCM data.</returns>
        public static byte[] EncodeToPCM(this AudioClip audioClip, PCMFormatSize size = PCMFormatSize.SixteenBit, bool trim = false, int outputSampleRate = 44100)
        {
            var samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            if (audioClip.frequency != outputSampleRate)
            {
                samples = PCMEncoder.Resample(samples, null, audioClip.frequency, outputSampleRate);
            }

            return PCMEncoder.Encode(samples, size, trim);
        }

        /// <summary>
        /// Decodes the raw PCM byte data and sets it to the <see cref="AudioClip"/>.
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="inputSampleRate">The sample rate of the <see cref="pcmData"/> provided.</param>
        public static void DecodeFromPCM(this AudioClip audioClip, byte[] pcmData, PCMFormatSize size = PCMFormatSize.SixteenBit, int inputSampleRate = 44100)
        {
            var samples = PCMEncoder.Decode(pcmData, size, inputSampleRate, 44100);
            // Set the decoded audio data directly into the existing AudioClip
            audioClip.SetData(samples, 0);
        }
    }
}

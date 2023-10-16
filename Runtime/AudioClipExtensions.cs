// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

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
            audioClip.GetData(samples, 0);
            return PCMEncoder.Encode(samples, size, trim);
        }

        /// <summary>
        /// Decodes the raw PCM byte data and sets it to the <see cref="AudioClip"/>.<br/>
        /// </summary>
        /// <param name="audioClip"><see cref="AudioClip"/>.</param>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        public static void DecodeFromPCM(this AudioClip audioClip, byte[] pcmData, PCMFormatSize size = PCMFormatSize.EightBit)
        {
            var samples = PCMEncoder.Decode(pcmData, size);
            // Set the decoded audio data directly into the existing AudioClip
            audioClip.SetData(samples, 0);
        }
    }
}

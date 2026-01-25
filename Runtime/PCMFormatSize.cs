// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.Audio
{
    /// <summary>
    /// Specifies the bit depth of PCM audio samples.
    /// </summary>
    public enum PCMFormatSize
    {
        /// <summary>
        /// 8-bit PCM format (1 byte per sample).
        /// </summary>
        EightBit = 1,
        /// <summary>
        /// 16-bit PCM format (2 bytes per sample).
        /// </summary>
        SixteenBit = 2,
        /// <summary>
        /// 24-bit PCM format (3 bytes per sample).
        /// </summary>
        TwentyFourBit = 3,
        /// <summary>
        /// 32-bit PCM format (4 bytes per sample).
        /// </summary>
        ThirtyTwoBit = 4
    }
}

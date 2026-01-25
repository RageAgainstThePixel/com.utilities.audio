// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.Audio
{
    /// <summary>
    /// A streaming recording behaviour implementation using <see cref="PCMEncoder"/>.
    /// </summary>
    /// <remarks>
    /// This component provides real-time recording functionality with PCM audio encoding
    /// and callback support for processing audio samples during recording.
    /// It extends <see cref="AbstractStreamRecordingBehaviour{TEncoder}"/> for streaming support.
    /// </remarks>
    public class PCMStreamRecordingBehaviour : AbstractStreamRecordingBehaviour<PCMEncoder>
    {
    }
}

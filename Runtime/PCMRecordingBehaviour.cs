// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.Audio
{
    /// <summary>
    /// A recording behaviour implementation using <see cref="PCMEncoder"/>.
    /// </summary>
    /// <remarks>
    /// This component provides recording functionality with PCM audio encoding.
    /// It extends <see cref="AbstractRecordingBehaviour{TEncoder}"/> for easy integration in scenes.
    /// </remarks>
    public class PCMRecordingBehaviour : AbstractRecordingBehaviour<PCMEncoder>
    {
    }
}

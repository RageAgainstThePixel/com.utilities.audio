// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Utilities.Audio
{
    public interface IEncoder
    {
        /// <summary>
        /// Streams audio microphone recording input to memory.
        /// </summary>
        /// <param name="microphoneClipData">The microphone input clip data.</param>
        /// <param name="bufferCallback">The event raised when buffer data is ready to write.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <param name="callingMethodName">Used to determine where this method was called from.</param>
        Task StreamRecordingAsync(ClipData microphoneClipData, Func<ReadOnlyMemory<byte>, Task> bufferCallback, CancellationToken cancellationToken, [CallerMemberName] string callingMethodName = null);

        /// <summary>
        /// Streams audio microphone recording input to disk.
        /// </summary>
        /// <param name="microphoneClipData">The microphone input clip data.</param>
        /// <param name="saveDirectory">The save directory to create the new audio file.</param>
        /// <param name="callback">The event that is raised when an audio clip has finished recording and has been saved to disk.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <param name="callingMethodName">Used to determine where this method was called from.</param>
        /// <returns>A <see cref="Tuple{Tstring, TAudioClip}"/> containing the path to the recorded file, and the full <see cref="AudioClip"/> recording.</returns>
        Task<Tuple<string, AudioClip>> StreamSaveToDiskAsync(ClipData microphoneClipData, string saveDirectory, Action<Tuple<string, AudioClip>> callback, CancellationToken cancellationToken, [CallerMemberName] string callingMethodName = null);
    }
}

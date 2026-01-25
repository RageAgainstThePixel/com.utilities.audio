// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Utilities.Audio
{
    /// <summary>
    /// Contains metadata and information about an audio clip used for recording.
    /// </summary>
    public class ClipData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClipData"/> class.
        /// </summary>
        /// <param name="clip">The audio clip being recorded.</param>
        /// <param name="recordingDevice">The name of the recording device.</param>
        /// <param name="outputSampleRate">The desired output sample rate.</param>
        public ClipData(AudioClip clip, string recordingDevice, int outputSampleRate)
        {
            Clip = clip;
            Name = clip.name;
            Device = recordingDevice;
            Channels = clip.channels;
            InputBufferSize = clip.samples;
            InputSampleRate = clip.frequency;
            OutputSampleRate = outputSampleRate;
        }

        /// <summary>
        /// Gets the audio clip being recorded.
        /// </summary>
        public AudioClip Clip { get; }

        /// <summary>
        /// Gets the name of the recording device.
        /// </summary>
        public string Device { get; }

        /// <summary>
        /// Gets the name of the audio clip.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the number of audio channels.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets the buffer size in samples.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <see cref="InputBufferSize"/>.
        /// </remarks>
        [Obsolete("use InputBufferSize")]
        public int BufferSize => InputBufferSize;

        /// <summary>
        /// Gets the input sample rate in Hz.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <see cref="InputSampleRate"/>.
        /// </remarks>
        [Obsolete("use InputSampleRate")]
        public int SampleRate => InputSampleRate;

        /// <summary>
        /// Gets the input sample rate in Hz.
        /// </summary>
        public int InputSampleRate { get; }

        /// <summary>
        /// Gets the buffer size in samples.
        /// </summary>
        public int InputBufferSize { get; }

        /// <summary>
        /// Gets the output sample rate in Hz.
        /// </summary>
        public int OutputSampleRate { get; }

        /// <summary>
        /// Gets or sets the maximum number of samples to record.
        /// </summary>
        public int? MaxSamples { get; internal set; }
    }
}

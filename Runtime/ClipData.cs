// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Utilities.Audio
{
    public class ClipData
    {
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

        public AudioClip Clip { get; }

        public string Device { get; }

        public string Name { get; }

        public int Channels { get; }

        [Obsolete("use InputBufferSize")]
        public int BufferSize => InputBufferSize;

        [Obsolete("use InputSampleRate")]
        public int SampleRate => InputSampleRate;

        public int InputSampleRate { get; }

        public int InputBufferSize { get; }

        public int OutputSampleRate { get; }

        public int? MaxSamples { get; internal set; }
    }
}

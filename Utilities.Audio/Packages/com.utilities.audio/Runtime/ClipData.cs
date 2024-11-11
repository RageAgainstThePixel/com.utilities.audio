// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Utilities.Audio
{
    public class ClipData
    {
        public ClipData(AudioClip clip, string recordingDevice)
        {
            Clip = clip;
            Name = clip.name;
            Device = recordingDevice;
            Channels = clip.channels;
            BufferSize = clip.samples;
            SampleRate = clip.frequency;
            MaxSamples = RecordingManager.MaxRecordingLength * SampleRate;
        }

        public AudioClip Clip { get; }

        public string Device { get; }

        public string Name { get; }

        public int Channels { get; }

        public int BufferSize { get; }

        public int SampleRate { get; }

        public int MaxSamples { get; }
    }
}

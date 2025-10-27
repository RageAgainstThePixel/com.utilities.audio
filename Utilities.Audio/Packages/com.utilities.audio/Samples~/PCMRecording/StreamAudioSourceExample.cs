// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities.Audio;

namespace Utilities.Encoder.PCM.Samples.Recording
{
    /// <summary>
    /// This class shows an example of how to use the StreamAudioSource component.
    /// We will create an audio buffer and set samples to it.
    /// We will then play the audio buffer using the StreamAudioSource component.
    /// </summary>
    [RequireComponent(typeof(StreamAudioSource))]
    public class StreamAudioSourceExample : MonoBehaviour
    {
        [SerializeField]
        private StreamAudioSource streamAudioSource;

        private NativeArray<float> sampleBuffer;

        private float lastTimestamp;

        private void OnValidate()
        {
            if (streamAudioSource == null)
            {
                streamAudioSource = GetComponent<StreamAudioSource>();
            }
        }

        private void Awake()
        {
            OnValidate();
            sampleBuffer = new NativeArray<float>(AudioSettings.outputSampleRate, Allocator.Persistent);
            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                sampleBuffer[i] = math.sin(2 * Mathf.PI * 440 * i / AudioSettings.outputSampleRate);
            }
        }

        private void Update()
        {
            if (Time.time - lastTimestamp >= .9f)
            {
                lastTimestamp = Time.time;
                streamAudioSource.BufferCallback(sampleBuffer);
            }
        }

        private void OnDestroy()
        {
            sampleBuffer.Dispose();
        }
    }
}

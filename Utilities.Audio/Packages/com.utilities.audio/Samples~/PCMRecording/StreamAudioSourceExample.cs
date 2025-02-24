// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        private float[] sampleBuffer;

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
            sampleBuffer = new float[AudioSettings.outputSampleRate];

            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                sampleBuffer[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / AudioSettings.outputSampleRate);
            }
        }

        private void Update()
        {
            // we will do slightly less than a second
            // to make sure the buffer isn't lagging behind
            if (Time.time - lastTimestamp >= .9f)
            {
                lastTimestamp = Time.time;
                streamAudioSource.BufferCallback(sampleBuffer);
            }
        }
    }
}

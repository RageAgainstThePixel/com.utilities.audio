// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Utilities.Audio
{
    /// <summary>
    /// A simple implementation of a recording behaviour.
    /// </summary>
    /// <typeparam name="TEncoder"><see cref="IEncoder"/> to use for recording.</typeparam>
    [RequireComponent(typeof(AudioSource))]
    public abstract class AbstractStreamRecordingBehaviour<TEncoder> : MonoBehaviour where TEncoder : IEncoder
    {
        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private KeyCode recordingKey = KeyCode.Space;

        [SerializeField]
        private bool debug;

        public bool Debug
        {
            get => debug;
            set => debug = value;
        }

        [SerializeField]
        private SampleRates sampleRate = SampleRates.Hz44100;

        public enum SampleRates
        {
            Hz16000 = 16000,
            Hz24000 = 24000,
            Hz22050 = 22050,
            Hz44100 = 44100,
            Hz48000 = 48000,
            Hz96000 = 96000
        }

#if !UNITY_2022_1_OR_NEWER
        private CancellationTokenSource lifetimeCancellationTokenSource = new();
        // ReSharper disable once InconsistentNaming
        private CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif

        private readonly ConcurrentQueue<float> sampleQueue = new();

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (sampleQueue.Count < data.Length) { return; }

            for (var i = 0; i < data.Length; i += channels)
            {
                if (sampleQueue.TryDequeue(out var sample))
                {
                    for (var j = 0; j < channels; j++)
                    {
                        data[i + j] = sample;
                    }
                }
            }
        }

        private void Awake() => OnValidate();

        private void Start()
        {
            // Enable debugging
            RecordingManager.EnableDebug = debug;
        }

        private void Update()
        {
            if (Input.GetKeyUp(recordingKey))
            {
                if (RecordingManager.IsRecording)
                {
                    RecordingManager.EndRecording();
                }
                else
                {
                    if (!RecordingManager.IsBusy)
                    {
                        var recordingSampleRate = (int)sampleRate;
                        var playbackSampleRate = AudioSettings.outputSampleRate;

                        if (debug)
                        {
                            UnityEngine.Debug.Log($"recording sample rate: {recordingSampleRate}");
                            UnityEngine.Debug.Log($"playback sample rate: {playbackSampleRate}");
                        }

                        // ReSharper disable once MethodHasAsyncOverload
                        RecordingManager.StartRecordingStream<PCMEncoder>(BufferCallback, recordingSampleRate, destroyCancellationToken);

                        async Task BufferCallback(ReadOnlyMemory<byte> bufferCallback)
                        {
                            var samples = PCMEncoder.Decode(bufferCallback.ToArray(), inputSampleRate: recordingSampleRate, outputSampleRate: playbackSampleRate);

                            foreach (var sample in samples)
                            {
                                sampleQueue.Enqueue(sample);
                            }

                            await Task.Yield();
                        }
                    }
                }
            }
        }

#if !UNITY_2022_1_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
        }
#endif
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
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

#if PLATFORM_WEBGL
        private readonly ConcurrentQueue<AudioClip> audioQueue = new();
#else
        private readonly ConcurrentQueue<float> audioQueue = new();
#endif

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

#if PLATFORM_WEBGL
        private async void AudioPlaybackLoop()
        {
            try
            {
                do
                {
                    if (!audioSource.isPlaying &&
                        audioQueue.TryDequeue(out var clip))
                    {
                        try
                        {
                            UnityEngine.Debug.Log($"playing partial clip: {clip.name} | ({audioQueue.Count} remaining)");
                            audioSource.PlayOneShot(clip);
                            // ReSharper disable once MethodSupportsCancellation
                            await Task.Delay(TimeSpan.FromSeconds(clip.length)).ConfigureAwait(true);
                        }
                        finally
                        {
                            Destroy(clip);
                        }
                    }
                    else
                    {
                        await Task.Yield();
                    }
                } while (!destroyCancellationToken.IsCancellationRequested);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                // restart playback loop
                AudioPlaybackLoop();
            }
        }
#else
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (audioQueue.Count < data.Length) { return; }

            for (var i = 0; i < data.Length; i += channels)
            {
                if (audioQueue.TryDequeue(out var sample))
                {
                    for (var j = 0; j < channels; j++)
                    {
                        data[i + j] = sample;
                    }
                }
            }
        }
#endif

        private void Awake() => OnValidate();

        private void Start()
        {
            // Enable debugging
            RecordingManager.EnableDebug = debug;
            AudioPlaybackLoop();
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
                            // we need to resample the audio to match the playback sample rate of OnAudioFilterRead
                            // which should be the same as AudioSettings.outputSampleRate
                            var samples = PCMEncoder.Decode(bufferCallback.ToArray(), inputSampleRate: recordingSampleRate, outputSampleRate: playbackSampleRate);
#if PLATFORM_WEBGL
                            var clip = AudioClip.Create("microphone_recording", samples.Length, 1, AudioSettings.outputSampleRate, false);
                            clip.SetData(samples, 0);
                            audioQueue.Enqueue(clip);
#else
                            foreach (var sample in samples)
                            {
                                audioQueue.Enqueue(sample);
                            }
#endif

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

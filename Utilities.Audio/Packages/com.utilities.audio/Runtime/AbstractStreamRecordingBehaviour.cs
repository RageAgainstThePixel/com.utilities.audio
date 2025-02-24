// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Utilities.Audio
{
    /// <summary>
    /// A simple implementation of a recording behaviour.
    /// </summary>
    /// <typeparam name="TEncoder"><see cref="IEncoder"/> to use for recording.</typeparam>
    [RequireComponent(typeof(StreamAudioSource))]
    public abstract class AbstractStreamRecordingBehaviour<TEncoder> : MonoBehaviour where TEncoder : IEncoder
    {
        [SerializeField]
        private StreamAudioSource streamAudioSource;

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
        private System.Threading.CancellationTokenSource lifetimeCancellationTokenSource = new();
        // ReSharper disable once InconsistentNaming
        private System.Threading.CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif

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
                        RecordingManager.StartRecordingStream<PCMEncoder>(async audioData =>
                        {
                            await streamAudioSource.BufferCallbackAsync(audioData, recordingSampleRate, playbackSampleRate);
                        }, recordingSampleRate, destroyCancellationToken);
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

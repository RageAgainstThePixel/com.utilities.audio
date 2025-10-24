// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif // INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM

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

#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
        [SerializeField]
        private InputActionProperty inputActionRef = new(new InputAction("Record", InputActionType.Button, "<Keyboard>/space"));
#else
        [SerializeField]
        private KeyCode recordingKey = KeyCode.Space;
#endif // INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM

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
#endif // !UNITY_2022_1_OR_NEWER

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

        private void OnEnable()
        {
#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
            inputActionRef.action.Enable();
#endif // INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
        }

        private void Update()
        {
#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
            if (inputActionRef.action.WasPressedThisFrame())
#else
            if (Input.GetKeyUp(recordingKey))
#endif // INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
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

        private void OnDisable()
        {
#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
            inputActionRef.action.Enable();
#endif // INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
        }

#if !UNITY_2022_1_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
        }
#endif // !UNITY_2022_1_OR_NEWER
    }
}

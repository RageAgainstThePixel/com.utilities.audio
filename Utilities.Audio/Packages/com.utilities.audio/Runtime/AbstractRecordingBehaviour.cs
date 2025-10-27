// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
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
    [RequireComponent(typeof(AudioSource))]
    public abstract class AbstractRecordingBehaviour<TEncoder> : MonoBehaviour where TEncoder : IEncoder
    {
        [SerializeField]
        private AudioSource audioSource;

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
        private SampleRates sampleRate = SampleRates.Auto;

        private int recordingSampleRate = -1;

        public enum SampleRates
        {
            Auto = 0,
            Hz16000 = 16000,
            Hz24000 = 24000,
            Hz22050 = 22050,
            Hz44100 = 44100,
            Hz48000 = 48000,
            Hz96000 = 96000
        }

        public string DefaultSaveLocation
        {
            get => RecordingManager.DefaultSaveLocation;
            set => RecordingManager.DefaultSaveLocation = value;
        }

#if !UNITY_2022_1_OR_NEWER
        private System.Threading.CancellationTokenSource lifetimeCancellationTokenSource = new();
        // ReSharper disable once InconsistentNaming
        private System.Threading.CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif // !UNITY_2022_1_OR_NEWER

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Awake() => OnValidate();

        private void Start()
        {
            // Enable debugging
            RecordingManager.EnableDebug = debug;

            // Set the max recording length (min 30 seconds, max 300 seconds or 5 min)
            RecordingManager.MaxRecordingLength = 60;

            // Event raised whenever a recording is completed.
            RecordingManager.OnClipRecorded += OnClipRecorded;
        }

        private void OnEnable()
        {
#if INPUT_SYSTEM_EXISTS && ENABLE_INPUT_SYSTEM
            inputActionRef.action.Enable();
#endif // ENABLE_INPUT_SYSTEM
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
                    recordingSampleRate = -1;
                    RecordingManager.EndRecording();
                }
                else
                {
                    if (!RecordingManager.IsBusy)
                    {
                        StartRecording();
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
            lifetimeCancellationTokenSource.Dispose();
        }
#endif // !UNITY_2022_1_OR_NEWER

        private void OnClipRecorded(Tuple<string, AudioClip> recording)
        {
            recordingSampleRate = -1;

            var (path, newClip) = recording;

            if (debug)
            {
                UnityEngine.Debug.Log($"Recording saved at: {path}");
            }

            audioSource.PlayOneShot(newClip);
        }

        private async void StartRecording()
        {
            if (RecordingManager.IsBusy)
            {
                if (RecordingManager.IsRecording)
                {
                    UnityEngine.Debug.LogWarning("Recording in progress");
                }

                if (RecordingManager.IsProcessing)
                {
                    UnityEngine.Debug.LogWarning("Processing last recording");
                }

                return;
            }

            try
            {
                if (recordingSampleRate <= 0)
                {
                    if (sampleRate == 0)
                    {
                        UnityEngine.Microphone.GetDeviceCaps(RecordingManager.DefaultRecordingDevice, out _, out var max);
                        recordingSampleRate = max;
                    }
                    else
                    {
                        recordingSampleRate = (int)sampleRate;
                    }
                }

                // Starts the recording process
                var (path, newClip) = await RecordingManager.StartRecordingAsync<TEncoder>(
                    outputSampleRate: recordingSampleRate,
                    cancellationToken: destroyCancellationToken);

                if (debug)
                {
                    UnityEngine.Debug.Log($"Recording saved at: {path}");
                }

                audioSource.clip = newClip;
            }
            catch (TaskCanceledException)
            {
                // Do Nothing
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }
        }
    }
}

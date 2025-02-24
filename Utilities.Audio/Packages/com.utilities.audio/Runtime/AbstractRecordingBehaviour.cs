// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using UnityEngine;

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

        public string DefaultSaveLocation
        {
            get => RecordingManager.DefaultSaveLocation;
            set => RecordingManager.DefaultSaveLocation = value;
        }

#if !UNITY_2022_1_OR_NEWER
        private System.Threading.CancellationTokenSource lifetimeCancellationTokenSource = new();
        // ReSharper disable once InconsistentNaming
        private System.Threading.CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif

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
                        StartRecording();
                    }
                }
            }
        }

#if !UNITY_2022_1_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
            lifetimeCancellationTokenSource.Dispose();
        }
#endif

        private void OnClipRecorded(Tuple<string, AudioClip> recording)
        {
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
                // Starts the recording process
                var (path, newClip) = await RecordingManager.StartRecordingAsync<TEncoder>(outputSampleRate: (int)sampleRate, cancellationToken: destroyCancellationToken);

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

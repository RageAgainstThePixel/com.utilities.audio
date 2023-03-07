// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Utilities.Audio
{
    /// <summary>
    /// A simple implementation of a recording behaviour.
    /// </summary>
    /// <typeparam name="T"><see cref="IEncoder"/> to use for recording.</typeparam>
    [RequireComponent(typeof(AudioSource))]
    public abstract class AbstractRecordingBehaviour<T> : MonoBehaviour where T : IEncoder
    {
        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private KeyCode recordingKey = KeyCode.Space;

        private CancellationTokenSource gameObjectCts;

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
            RecordingManager.EnableDebug = true;

            // Set the default save location.
            RecordingManager.DefaultSaveLocation = $"{Application.streamingAssetsPath}/Resources/Recordings";

            // Set the max recording length (min 30 seconds, max 300 seconds or 5 min)
            RecordingManager.MaxRecordingLength = 60;

            // Event raised whenever a recording is completed.
            RecordingManager.OnClipRecorded += OnClipRecorded;

            // Stops the recording if this game object is destroyed.
            gameObjectCts = new CancellationTokenSource();
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

        private void OnDestroy()
        {
            gameObjectCts.Cancel();
        }

        private void OnClipRecorded(Tuple<string, AudioClip> recording)
        {
            var (path, newClip) = recording;
            Debug.Log($"Recording saved at: {path}");
            audioSource.PlayOneShot(newClip);
        }

        private async void StartRecording()
        {
            if (RecordingManager.IsBusy)
            {
                if (RecordingManager.IsRecording)
                {
                    Debug.Log("Recording in progress");
                }

                if (RecordingManager.IsProcessing)
                {
                    Debug.Log("Processing last recording");
                }

                return;
            }

            try
            {
                // Starts the recording process
                var recording = await RecordingManager.StartRecordingAsync<T>(cancellationToken: gameObjectCts.Token);
                var (path, newClip) = recording;
                Debug.Log($"Recording saved at: {path}");
                audioSource.clip = newClip;
            }
            catch (TaskCanceledException)
            {
                // Do Nothing
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}

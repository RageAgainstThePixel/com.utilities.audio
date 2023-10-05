// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;

namespace Utilities.Audio
{
    public static class RecordingManager
    {
        private static int maxRecordingLength = 300;

        private static readonly object recordingLock = new object();

        private static CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Max Recording length in seconds.
        /// The default value is 300 seconds (5 min)
        /// </summary>
        public static int MaxRecordingLength
        {
            get => maxRecordingLength;
            set
            {
                if (value != maxRecordingLength)
                {
                    if (value > 300)
                    {
                        maxRecordingLength = 300;
                    }
                    else if (value < 30)
                    {
                        maxRecordingLength = 30;
                    }
                    else
                    {
                        maxRecordingLength = value;
                    }
                }
            }
        }

        public static int Frequency { get; set; } = 48000;

        private static bool isRecording;

        /// <summary>
        /// Is the recording manager currently recording?
        /// </summary>
        public static bool IsRecording
        {
            get
            {
                bool recording;

                lock (recordingLock)
                {
                    recording = isRecording;
                }

                return recording;
            }
            internal set
            {
                if (value != isRecording)
                {
                    lock (recordingLock)
                    {
                        isRecording = value;
                    }
                }
            }
        }

        private static bool isProcessing;

        /// <summary>
        /// Is the recording manager currently processing the last recording?
        /// </summary>
        public static bool IsProcessing
        {
            get
            {
                bool processing;

                lock (recordingLock)
                {
                    processing = isProcessing;
                }

                return processing;
            }
            internal set
            {
                if (value != isProcessing)
                {
                    lock (recordingLock)
                    {
                        isProcessing = value;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates that the recording manager is either recording or processing the previous recording.
        /// </summary>
        public static bool IsBusy => IsProcessing || IsRecording;

        public static bool EnableDebug { get; set; }

        /// <summary>
        /// The event that is raised when an audio clip has finished recording and has been saved to disk.
        /// </summary>
        public static event Action<Tuple<string, AudioClip>> OnClipRecorded;

        private static string defaultSaveLocation;

        /// <summary>
        /// Defaults to /Assets/Resources/Recordings in editor.<br/>
        /// Defaults to /Application/TempCachePath/Recordings at runtime.
        /// </summary>
        public static string DefaultSaveLocation
        {
            get
            {
                if (string.IsNullOrWhiteSpace(defaultSaveLocation))
                {
#if UNITY_EDITOR
                    defaultSaveLocation = $"{Application.dataPath}/Resources/Recordings";
#else
                    defaultSaveLocation = $"{Application.temporaryCachePath}/Recordings";
#endif
                }

                return defaultSaveLocation;
            }
            set => defaultSaveLocation = value;
        }

        /// <summary>
        /// The default recording device to use for recording.
        /// </summary>
        public static string DefaultRecordingDevice { get; set; }

        /// <summary>
        /// Starts the recording process.
        /// </summary>
        /// <param name="clipName">Optional, name for the clip.</param>
        /// <param name="saveDirectory">Optional, the directory to save the clip.</param>
        /// <param name="callback">Optional, callback when recording is complete.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async void StartRecording<T>(string clipName = null, string saveDirectory = null, Action<Tuple<string, AudioClip>> callback = null, CancellationToken cancellationToken = default) where T : IEncoder
        {
            var result = await StartRecordingAsync<T>(clipName, saveDirectory, cancellationToken).ConfigureAwait(false);
            callback?.Invoke(result);
        }

        /// <summary>
        /// Starts the recording process.
        /// </summary>
        /// <param name="clipName">Optional, name for the clip.</param>
        /// <param name="saveDirectory">Optional, the directory to save the clip.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async Task<Tuple<string, AudioClip>> StartRecordingAsync<T>(string clipName = null, string saveDirectory = null, CancellationToken cancellationToken = default) where T : IEncoder
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[{nameof(RecordingManager)}] Recording already in progress!");

                return null;
            }

            lock (recordingLock)
            {
                isRecording = true;
            }

            if (string.IsNullOrWhiteSpace(saveDirectory))
            {
                saveDirectory = DefaultSaveLocation;
            }

            if (string.IsNullOrWhiteSpace(DefaultRecordingDevice))
            {
                DefaultRecordingDevice = null;
            }

            var clip = Microphone.Start(DefaultRecordingDevice, false, MaxRecordingLength, Frequency);

            if (EnableDebug)
            {
                var deviceName = string.IsNullOrWhiteSpace(DefaultRecordingDevice)
                    ? string.Join(", ", Microphone.devices)
                    : DefaultRecordingDevice;
                Microphone.GetDeviceCaps(DefaultRecordingDevice, out var minFreq, out var maxFreq);
                Debug.Log($"[{nameof(RecordingManager)}] Recording device(s): {deviceName} | minFreq: {minFreq} | maxFreq {maxFreq} | clip freq: {clip.frequency} | samples: {clip.samples}");
            }

            clip.name = (string.IsNullOrWhiteSpace(clipName) ? Guid.NewGuid().ToString() : clipName)!;
            clipName = clip.name;

            lock (recordingLock)
            {
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

#if UNITY_EDITOR
            if (EnableDebug)
            {
                Debug.Log($"[{nameof(RecordingManager)}] <>Disable auto refresh<>");
            }

            UnityEditor.AssetDatabase.DisallowAutoRefresh();
#endif

            try
            {
                var encoder = Activator.CreateInstance<T>();
                return await encoder.StreamSaveToDiskAsync(clip, saveDirectory, cancellationTokenSource.Token, OnClipRecorded).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] Failed to record {clipName}!\n{e}");
            }
            finally
            {
                lock (recordingLock)
                {
                    isRecording = false;
                    isProcessing = false;
                }
#if UNITY_EDITOR
                await Awaiters.UnityMainThread;

                if (EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] <>Enable auto refresh<>");
                }

                UnityEditor.AssetDatabase.AllowAutoRefresh();
#endif
            }

            return null;
        }

        /// <summary>
        /// Ends the recording process if in progress.
        /// </summary>
        public static void EndRecording()
        {
            if (!IsRecording)
            {
                return;
            }

            lock (recordingLock)
            {
                if (cancellationTokenSource is { IsCancellationRequested: false })
                {
                    cancellationTokenSource.Cancel();

                    if (EnableDebug)
                    {
                        Debug.Log($"[{nameof(RecordingManager)}] End Recording requested...");
                    }
                }
            }
        }
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Utilities.Async;

namespace Utilities.Audio
{
    public static class RecordingManager
    {
        private static readonly ConcurrentDictionary<Type, IEncoder> encoderCache = new();

        private static int maxRecordingLength = 300;

        private static readonly object recordingLock = new();

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

        [Obsolete("removed. Set OutputSamplingRate when requesting new recording.")]
        public static int Frequency { get; set; } = 44100;

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
#elif UNITY_WEBGL
                    defaultSaveLocation = string.Empty;
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

        [Obsolete("use new overload with outputSampleRate")]
        public static async void StartRecording<TEncoder>(string clipName, string saveDirectory, Action<Tuple<string, AudioClip>> callback, CancellationToken cancellationToken) where TEncoder : IEncoder
            => await StartRecordingAsync<TEncoder>(clipName, saveDirectory, 44100, cancellationToken).ConfigureAwait(true);

        /// <summary>
        /// Starts the recording process and saves a new clip to disk.
        /// </summary>
        /// <param name="clipName">Optional, name for the clip.</param>
        /// <param name="saveDirectory">Optional, the directory to save the clip.</param>
        /// <param name="outputSampleRate">The target output sample rate. Defaults to 44100.</param>
        /// <param name="callback">Optional, callback when recording is complete.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async void StartRecording<TEncoder>(string clipName = null, string saveDirectory = null, int outputSampleRate = 44100, Action<Tuple<string, AudioClip>> callback = null, CancellationToken cancellationToken = default) where TEncoder : IEncoder
        {
            var result = await StartRecordingAsync<TEncoder>(clipName, saveDirectory, outputSampleRate, cancellationToken).ConfigureAwait(true);
            callback?.Invoke(result);
        }

        [Obsolete("use new overload with outputSampleRate")]
        public static async Task<Tuple<string, AudioClip>> StartRecordingAsync<TEncoder>(string clipName, string saveDirectory, CancellationToken cancellationToken) where TEncoder : IEncoder
          => await StartRecordingAsync<TEncoder>(clipName, saveDirectory, 44100, cancellationToken).ConfigureAwait(true);

        /// <summary>
        /// Starts the recording process and saves a new clip to disk.
        /// </summary>
        /// <param name="clipName">Optional, name for the clip.</param>
        /// <param name="saveDirectory">Optional, the directory to save the clip.</param>
        /// <param name="outputSampleRate">The target output sample rate. Defaults to 44100.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async Task<Tuple<string, AudioClip>> StartRecordingAsync<TEncoder>(string clipName = null, string saveDirectory = null, int outputSampleRate = 44100, CancellationToken cancellationToken = default) where TEncoder : IEncoder
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[{nameof(RecordingManager)}] Recording already in progress!");
                return null;
            }

            var hasRecordingPermission = await Microphone.RequestRecordingPermissionsAsync(cancellationToken);

            if (!hasRecordingPermission)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] Microphone permissions denied!");
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

            Microphone.GetDeviceCaps(DefaultRecordingDevice, out var minFreq, out var maxFreq);

            using var micCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, micCts.Token);
            var timeoutToken = timeoutCts.Token;
            await new WaitUntil(() => Microphone.devices.Length > 0 || timeoutToken.IsCancellationRequested);

            if (Microphone.devices.Length == 0)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] No devices found to record from!");
                return null;
            }

            if (EnableDebug)
            {
                var deviceName = string.IsNullOrWhiteSpace(DefaultRecordingDevice)
                    ? string.Join(", ", Microphone.devices)
                    : DefaultRecordingDevice;
                Debug.Log($"[{nameof(RecordingManager)}] Recording device(s): {deviceName} | minFreq: {minFreq} | maxFreq {maxFreq}");
            }

            // create dummy clip for recording purposes with a 1-second buffer.
            var clip = Microphone.Start(DefaultRecordingDevice, loop: true, length: 1, outputSampleRate);

            if (clip == null)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] Failed to initialize Unity Microphone!");
                return null;
            }

            clip.name = (string.IsNullOrWhiteSpace(clipName) ? Guid.NewGuid().ToString() : clipName)!;
            clipName = clip.name;

            if (EnableDebug)
            {
                Debug.Log($"Created new clip {clip.name} | clip freq: {clip.frequency} | samples: {clip.samples}");
            }

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
                if (!encoderCache.TryGetValue(typeof(TEncoder), out var encoder))
                {
                    encoder = Activator.CreateInstance<TEncoder>();
                    encoderCache.TryAdd(typeof(TEncoder), encoder);
                }

                var clipData = InitializeRecording(clip, outputSampleRate);
                clipData.MaxSamples = MaxRecordingLength * clip.frequency;
                return await encoder.StreamSaveToDiskAsync(clipData, saveDirectory, OnClipRecorded, cancellationTokenSource.Token);
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

                await Awaiters.UnityMainThread;

#if UNITY_EDITOR

                if (EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] <>Enable auto refresh<>");
                }

                UnityEditor.AssetDatabase.AllowAutoRefresh();
#endif
            }

            return null;
        }

        [Obsolete("use new overload with outputSampleRate")]
        public static async void StartRecordingStream<TEncoder>(Func<NativeArray<byte>, Task> bufferCallback, CancellationToken cancellationToken)
            where TEncoder : IEncoder
            => await StartRecordingStreamAsync<TEncoder>(bufferCallback, 44100, cancellationToken).ConfigureAwait(true);

        /// <summary>
        /// Starts the recording process and buffers the samples back as <see cref="ReadOnlyMemory{Tbytes}"/>.
        /// </summary>
        /// <typeparam name="TEncoder"><see cref="IEncoder"/>.</typeparam>
        /// <param name="bufferCallback">The buffer callback with new sample data.</param>
        /// <param name="outputSampleRate">The target output sample rate. Defaults to 44100.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async void StartRecordingStream<TEncoder>(Func<NativeArray<byte>, Task> bufferCallback, int outputSampleRate = 44100, CancellationToken cancellationToken = default)
            where TEncoder : IEncoder
            => await StartRecordingStreamAsync<TEncoder>(bufferCallback, outputSampleRate, cancellationToken).ConfigureAwait(true);

        /// <summary>
        /// Starts the recording process and buffers the samples back as <see cref="ReadOnlyMemory{Tbytes}"/>.
        /// </summary>
        /// <typeparam name="TEncoder"><see cref="IEncoder"/>.</typeparam>
        /// <param name="bufferCallback">The buffer callback with new sample data.</param>
        /// <param name="outputSampleRate">The target output sample rate. Defaults to 44100.</param>
        /// <param name="cancellationToken">Optional, task cancellation token.</param>
        public static async Task StartRecordingStreamAsync<TEncoder>(Func<NativeArray<byte>, Task> bufferCallback, int outputSampleRate = 44100, CancellationToken cancellationToken = default)
            where TEncoder : IEncoder
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[{nameof(RecordingManager)}] Recording already in progress!");
                return;
            }

            var hasRecordingPermission = await Microphone.RequestRecordingPermissionsAsync(cancellationToken);

            if (!hasRecordingPermission)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] Microphone permissions denied!");
                return;
            }

            lock (recordingLock)
            {
                isRecording = true;
            }

            if (string.IsNullOrWhiteSpace(DefaultRecordingDevice))
            {
                DefaultRecordingDevice = null;
            }

            Microphone.GetDeviceCaps(DefaultRecordingDevice, out var minFreq, out var maxFreq);

            using var micCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, micCts.Token);
            var timeoutToken = timeoutCts.Token;
            await new WaitUntil(() => Microphone.devices.Length > 0 || timeoutToken.IsCancellationRequested);

            if (Microphone.devices.Length == 0)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] No devices found to record from!");
                return;
            }

            if (EnableDebug)
            {
                var deviceName = string.IsNullOrWhiteSpace(DefaultRecordingDevice)
                    ? string.Join(", ", Microphone.devices)
                    : DefaultRecordingDevice;
                Debug.Log($"[{nameof(RecordingManager)}] Recording device(s): {deviceName} | minFreq: {minFreq} | maxFreq {maxFreq}");
            }

            // create dummy clip for recording purposes with a 1-second buffer.
            var clip = Microphone.Start(DefaultRecordingDevice, loop: true, length: 1, outputSampleRate);

            if (clip == null)
            {
                Debug.LogError($"[{nameof(RecordingManager)}] Failed to initialize Unity Microphone!");
                return;
            }

            clip.name = Guid.NewGuid().ToString();
            var clipName = clip.name;

            if (EnableDebug)
            {
                Debug.Log($"Created new clip {clip.name} | clip freq: {clip.frequency} | samples: {clip.samples}");
            }

            lock (recordingLock)
            {
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            try
            {
                if (!encoderCache.TryGetValue(typeof(TEncoder), out var encoder))
                {
                    encoder = Activator.CreateInstance<TEncoder>();
                    encoderCache.TryAdd(typeof(TEncoder), encoder);
                }

                await encoder.StreamRecordingAsync(InitializeRecording(clip, outputSampleRate), bufferCallback, cancellationTokenSource.Token);
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

                await Awaiters.UnityMainThread;
            }
        }

        /// <summary>
        /// Ends the recording process if in progress.
        /// </summary>
        public static void EndRecording()
        {
            if (!IsRecording)
            {
                Debug.LogWarning("No recording in progress!");
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

        private static ClipData InitializeRecording(AudioClip micInputClip, int outputSampleRate)
        {
            var device = DefaultRecordingDevice;

            if (!Microphone.IsRecording(device))
            {
                throw new InvalidOperationException("Microphone is not initialized!");
            }

            if (IsProcessing)
            {
                throw new AccessViolationException("Recording already in progress!");
            }

            var clipData = new ClipData(micInputClip, device, outputSampleRate);

            if (EnableDebug)
            {
                Debug.Log($"[{nameof(RecordingManager)}] Initializing data for {clipData.Name}. Channels: {clipData.Channels}, Input Sample Rate: {clipData.InputSampleRate}, Output Sample Rate: {clipData.OutputSampleRate} Sample buffer size: {clipData.InputBufferSize}, Max Sample Length: {clipData.MaxSamples}");
            }

            return clipData;
        }
    }
}

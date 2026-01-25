// Licensed under the MIT License. See LICENSE in the project root for license information.

using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;

#if !UNITY_2022_1_OR_NEWER
using System.Threading;
#endif // !UNITY_2022_1_OR_NEWER

namespace Utilities.Audio
{
    /// <summary>
    /// Streams audio and plays it back on the attached <see cref="AudioSource"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class StreamAudioSource : MonoBehaviour
    {
        /// <summary>
        /// Implicitly converts a <see cref="StreamAudioSource"/> to its attached <see cref="AudioSource"/>.
        /// </summary>
        /// <param name="streamAudioSource">The stream audio source component.</param>
        /// <returns>The attached audio source.</returns>
        [Preserve]
        public static implicit operator AudioSource(StreamAudioSource streamAudioSource)
            => streamAudioSource.audioSource;

        [SerializeField]
        private AudioSource audioSource;

#if !UNITY_2022_1_OR_NEWER
        private CancellationTokenSource lifetimeCancellationTokenSource = new();

        // ReSharper disable once InconsistentNaming
        private CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif // !UNITY_2022_1_OR_NEWER

        private NativeQueue<float> audioQueue;

        private void EnsureQueueInitialized()
        {
            if (!audioQueue.IsCreated)
            {
                audioQueue = new NativeQueue<float>(Allocator.Persistent);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the audio queue is empty.
        /// </summary>
        /// <remarks>
        /// If the queue has not been initialized yet, this returns true.
        /// </remarks>
        public bool IsEmpty
        {
            get
            {
                // Lazy initialization in case accessed before Awake
                if (!audioQueue.IsCreated)
                {
                    return true;
                }

                return audioQueue.Count == 0;
            }
        }

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Awake()
        {
            EnsureQueueInitialized();
            OnValidate();
#if PLATFORM_WEBGL && !UNITY_EDITOR
            AudioPlaybackLoop();
#endif // PLATFORM_WEBGL && !UNITY_EDITOR
        }

#if PLATFORM_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern IntPtr AudioStream_InitPlayback(int playbackSampleRate);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int AudioStream_AppendBufferPlayback(IntPtr audioContextPtr, float[] buffer, int length);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int AudioStream_SetVolume(IntPtr audioContextPtr, float volume);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern IntPtr AudioStream_Dispose(IntPtr audioContextPtr);

        [Preserve]
        private async void AudioPlaybackLoop()
        {
            //Debug.Log($"Start {nameof(AudioPlaybackLoop)}");

            var audioContextPtr = AudioStream_InitPlayback(AudioSettings.outputSampleRate);
            var buffer = new float[AudioSettings.outputSampleRate];
            var bufferLength = buffer.Length;

            try
            {
                if (audioContextPtr == IntPtr.Zero)
                {
                    throw new Exception("Failed to initialize a new audio context!");
                }

                while (!destroyCancellationToken.IsCancellationRequested)
                {
                    //Debug.Log($"AudioStream_SetVolume::volume:{audioSource.volume}");
                    AudioStream_SetVolume(audioContextPtr, audioSource.volume);

                    var count = 0;

                    for (var i = 0; i < bufferLength; i++)
                    {
                        if (audioQueue.TryDequeue(out var sample))
                        {
                            buffer[i] = sample;
                            count++;
                        }
                        else
                        {
                            // Zero remaining buffer elements to prevent stale samples on underrun
                            Array.Clear(buffer, i, bufferLength - i);
                            break;
                        }
                    }

                    if (count > 0)
                    {
                        //Debug.Log($"AudioStream_AppendBufferPlayback::bufferLength:{count}");
                        AudioStream_AppendBufferPlayback(audioContextPtr, buffer, count);
                    }

                    await Task.Yield();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                AudioStream_Dispose(audioContextPtr);
            }
        }
#else
        /// <summary>
        /// Processes audio samples for playback. Expects mono samples which are duplicated across all channels.
        /// Buffers are zeroed at start of each frame to prevent stale samples on queue underrun.
        /// </summary>
        /// <param name="data">Audio buffer to fill. Will be cleared before filling to ensure silence on underrun.</param>
        /// <param name="channels">Number of audio channels to fill (all channels receive the same mono sample).</param>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            try
            {
                var length = data.Length;

                // Only attempt dequeue if queue has been initialized
                if (!audioQueue.IsCreated)
                {
                    // Clear entire buffer if queue was never initialized
                    Array.Clear(data, 0, length);
                    return;
                }

                for (var i = 0; i < length; i += channels)
                {
                    if (audioQueue.TryDequeue(out var sample))
                    {
                        for (var j = 0; j < channels; j++)
                        {
                            data[i + j] = sample;
                        }
                    }
                    else
                    {
                        // Break on first underrun and only clear remaining buffer elements
                        Array.Clear(data, i, length - i);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
#endif // PLATFORM_WEBGL && !UNITY_EDITOR

        private void OnDestroy()
        {
#if !UNITY_2022_1_OR_NEWER
            if (lifetimeCancellationTokenSource is { IsCancellationRequested: false })
            {
                lifetimeCancellationTokenSource.Cancel();
            }
#endif // !UNITY_2022_1_OR_NEWER
            // Properly dispose of the queue to release Persistent allocator memory
            if (audioQueue.IsCreated)
            {
                audioQueue.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously queues audio samples for playback. Exceptions are logged to debug output.
        /// </summary>
        /// <param name="samples">Array of audio samples to queue.</param>
        /// <param name="count">Optional number of samples to queue. Defaults to array length.</param>
        /// <param name="inputSampleRate">Optional input sample rate for resampling.</param>
        /// <param name="outputSampleRate">Optional output sample rate for resampling.</param>
        [UsedImplicitly]
        public async void SampleCallback(float[] samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            try
            {
                await SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Asynchronously queues audio samples for playback with resampling support.
        /// </summary>
        /// <param name="samples">Array of audio samples to queue.</param>
        /// <param name="count">Optional number of samples to queue. Defaults to array length.</param>
        /// <param name="inputSampleRate">Optional input sample rate for resampling.</param>
        /// <param name="outputSampleRate">Optional output sample rate for resampling.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [UsedImplicitly]
        public Task SampleCallbackAsync(float[] samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            var native = new NativeArray<float>(samples, Allocator.Persistent);

            try
            {
                return SampleCallbackAsync(native, count, inputSampleRate, outputSampleRate);
            }
            finally
            {
                native.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously queues native audio samples for playback. Exceptions are logged to debug output.
        /// </summary>
        /// <param name="samples">Native array of audio samples to queue.</param>
        /// <param name="count">Optional number of samples to queue. Defaults to array length.</param>
        /// <param name="inputSampleRate">Optional input sample rate for resampling.</param>
        /// <param name="outputSampleRate">Optional output sample rate for resampling.</param>
        [UsedImplicitly]
        public async void SampleCallback(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            try
            {
                await SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Asynchronously queues native audio samples for playback with resampling support.
        /// </summary>
        /// <param name="samples">Native array of audio samples to queue.</param>
        /// <param name="count">Optional number of samples to queue. Defaults to array length.</param>
        /// <param name="inputSampleRate">Optional input sample rate for resampling.</param>
        /// <param name="outputSampleRate">Optional output sample rate for resampling.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [UsedImplicitly]
        public Task SampleCallbackAsync(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            if (inputSampleRate.HasValue &&
                outputSampleRate.HasValue &&
                inputSampleRate != outputSampleRate)
            {
                // Resampling required: create new NativeArray from resampler output
                var resampled = PCMEncoder.Resample(samples, inputSampleRate.Value, outputSampleRate.Value, Allocator.Persistent);
                try
                {
                    return Enqueue(resampled, count ?? resampled.Length);
                }
                finally
                {
                    resampled.Dispose();
                }
            }

            // No resampling needed: enqueue directly without copying to maintain zero-allocation design
            return Enqueue(samples, count ?? samples.Length);
        }

        /// <summary>
        /// Asynchronously processes PCM buffer data for playback with resampling support.
        /// </summary>
        /// <param name="pcmData">Raw PCM byte data to process.</param>
        /// <param name="inputSampleRate">The input sample rate of the PCM data.</param>
        /// <param name="outputSampleRate">The desired output sample rate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [UsedImplicitly]
        public Task BufferCallbackAsync(NativeArray<byte> pcmData, int inputSampleRate, int outputSampleRate)
        {
            var samples = PCMEncoder.Decode(pcmData, inputSampleRate: inputSampleRate, outputSampleRate: outputSampleRate, allocator: Allocator.Persistent);

            try
            {
                return Enqueue(samples, samples.Length);
            }
            finally
            {
                samples.Dispose();
            }
        }

        private Task Enqueue(NativeArray<float> samples, int count)
        {
            EnsureQueueInitialized();

            for (var i = 0; i < count; i++)
            {
                audioQueue.Enqueue(samples[i]);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all queued audio samples.
        /// </summary>
        [UsedImplicitly]
        public void ClearBuffer()
        {
            if (audioQueue.IsCreated)
            {
                audioQueue.Clear();
            }
        }
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;

#if PLATFORM_WEBGL  && !UNITY_EDITOR
using System;
#endif // PLATFORM_WEBGL && !UNITY_EDITOR

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

        private float[] resampleBuffer;

        public bool IsEmpty => audioQueue.Count == 0;

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Awake()
        {
            audioQueue = new NativeQueue<float>(Allocator.Persistent);
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
        private void OnAudioFilterRead(float[] data, int channels)
        {
            try
            {
                var length = data.Length;

                for (var i = 0; i < length; i += channels)
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
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
#endif // PLATFORM_WEBGL && !UNITY_EDITOR

        private void OnDestroy()
        {
#if !UNITY_2022_1_OR_NEWER
            lifetimeCancellationTokenSource.Cancel();
#endif // !UNITY_2022_1_OR_NEWER
            audioQueue.Dispose();
        }

        public void SampleCallback(float[] samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
            => SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate).ConfigureAwait(true);

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

        public void SampleCallback(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
            => SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate).ConfigureAwait(true);

        public Task SampleCallbackAsync(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            NativeArray<float>? resampled = null;

            if (inputSampleRate.HasValue && outputSampleRate.HasValue && inputSampleRate != outputSampleRate)
            {
                resampled = PCMEncoder.Resample(samples, inputSampleRate.Value, outputSampleRate.Value, Allocator.Persistent);
            }

            try
            {
                return Enqueue(resampled ?? samples, count ?? samples.Length);
            }
            finally
            {
                resampled?.Dispose();
            }

        }

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
            for (var i = 0; i < count; i++)
            {
                audioQueue.Enqueue(samples[i]);
            }

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        public void ClearBuffer()
            => audioQueue.Clear();
    }
}

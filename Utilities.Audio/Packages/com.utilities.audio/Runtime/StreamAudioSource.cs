// Licensed under the MIT License. See LICENSE in the project root for license information.

using JetBrains.Annotations;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

#if PLATFORM_WEBGL && !UNITY_EDITOR
using System;
#endif // PLATFORM_WEBGL && !UNITY_EDITOR

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

        private NativeQueue<float> audioBuffer;

        private float[] resampleBuffer;

        public bool IsEmpty => audioBuffer.Count == 0;

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Awake()
        {
            audioBuffer = new NativeQueue<float>(Allocator.Persistent);
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

                    var bufferLength = buffer.Length;

                    if (audioBuffer.Count >= bufferLength)
                    {
                        var count = 0;

                        for (var i = 0; i < bufferLength; i++)
                        {
                            if (audioBuffer.TryDequeue(out var sample))
                            {
                                buffer[i] = sample;
                                count++;
                            }
                        }

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
            if (!audioBuffer.IsCreated || audioBuffer.Count < data.Length) { return; }

            for (var i = 0; i < data.Length; i += channels)
            {
                if (audioBuffer.TryDequeue(out var sample))
                {
                    for (var j = 0; j < channels; j++)
                    {
                        data[i + j] = sample;
                    }
                }
            }
        }
#endif // PLATFORM_WEBGL && !UNITY_EDITOR

        private void OnDestroy()
        {
#if !UNITY_2022_1_OR_NEWER
            lifetimeCancellationTokenSource.Cancel();
#endif // !UNITY_2022_1_OR_NEWER
            audioBuffer.Dispose();
        }

        public void SampleCallback(float[] samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
            => SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate).ConfigureAwait(false);

        public Task SampleCallbackAsync(float[] samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
            => SampleCallbackAsync(new NativeArray<float>(samples, Allocator.Temp), count, inputSampleRate, outputSampleRate);

        public void SampleCallback(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
            => SampleCallbackAsync(samples, count, inputSampleRate, outputSampleRate).ConfigureAwait(false);

        public Task SampleCallbackAsync(NativeArray<float> samples, int? count = null, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            if (inputSampleRate.HasValue && outputSampleRate.HasValue && inputSampleRate != outputSampleRate)
            {
                samples = PCMEncoder.Resample(samples, inputSampleRate.Value, outputSampleRate.Value);
            }

            count ??= samples.Length;
            return Enqueue(samples, count.Value);
        }

        public Task BufferCallbackAsync(NativeArray<byte> pcmData, int inputSampleRate, int outputSampleRate)
        {
            var samples = PCMEncoder.Decode(pcmData, inputSampleRate: inputSampleRate, outputSampleRate: outputSampleRate);
            return Enqueue(samples, samples.Length);
        }

        private Task Enqueue(NativeArray<float> samples, int count)
        {
            for (var i = 0; i < count; i++)
            {
                audioBuffer.Enqueue(samples[i]);
            }

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        public void ClearBuffer()
            => audioBuffer.Clear();
    }
}

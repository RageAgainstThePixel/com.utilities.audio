using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace Utilities.Audio
{
    /// <summary>
    /// Streams audio and plays it back on the attached <see cref="AudioSource"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class StreamAudioSource : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;

#if !UNITY_2022_1_OR_NEWER
        private CancellationTokenSource lifetimeCancellationTokenSource = new();
        // ReSharper disable once InconsistentNaming
        private CancellationToken destroyCancellationToken => lifetimeCancellationTokenSource.Token;
#endif

        private readonly ConcurrentQueue<float> audioBuffer = new();

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Awake()
        {
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
            var audioContextPtr = AudioStream_InitPlayback(AudioSettings.outputSampleRate);
            var buffer = new float[AudioSettings.outputSampleRate];

            try
            {
                while (!destroyCancellationToken.IsCancellationRequested)
                {
                    AudioStream_SetVolume(audioContextPtr, audioSource.volume);

                    if (!audioBuffer.IsEmpty)
                    {
                        var bufferLength = 0;

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (audioBuffer.TryDequeue(out var sample))
                            {
                                buffer[i] = sample;
                                bufferLength++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        AudioStream_AppendBufferPlayback(audioContextPtr, buffer, bufferLength);
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
            if (audioBuffer.Count < data.Length) { return; }

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

#if !UNITY_2022_1_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
        }
#endif

        [Preserve]
        public async Task BufferCallback(float[] bufferCallback)
        {
            foreach (var sample in bufferCallback)
            {
                audioBuffer.Enqueue(sample);
            }

            await Task.Yield();
        }

        [Preserve]
        public async Task BufferCallback(ReadOnlyMemory<byte> audioData, int inputSampleRate, int outputSampleRate)
        {
            var samples = PCMEncoder.Decode(audioData.ToArray(), inputSampleRate: inputSampleRate, outputSampleRate: outputSampleRate);

            foreach (var sample in samples)
            {
                audioBuffer.Enqueue(sample);
            }

            await Task.Yield();
        }
    }
}

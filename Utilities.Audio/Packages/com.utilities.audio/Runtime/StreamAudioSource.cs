using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
            => OnValidate();

#if PLATFORM_WEBGL
        [DllImport("__Internal")]
        private static extern IntPtr InitAudioStreamPlayback(int playbackSampleRate);

        [DllImport("__Internal")]
        private static extern void AppendPlaybackBuffer(IntPtr audioContextPtr, byte[] buffer, int length);

        [DllImport("__Internal")]
        private static extern void SetVolume(IntPtr audioContextPtr, float volume);

        [DllImport("__Internal")]
        private static extern IntPtr Dispose(IntPtr audioContextPtr);

        private async void AudioPlaybackLoop()
        {
            var audioContextPtr = InitAudioStreamPlayback(AudioSettings.outputSampleRate);

            try
            {
                while (!destroyCancellationToken.IsCancellationRequested)
                {
                    SetVolume(audioContextPtr, audioSource.volume);
                    await Task.Yield();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {

                if (audioContextPtr != IntPtr.Zero)
                {
                    Dispose(audioContextPtr);
                }
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
#endif

#if !UNITY_2022_1_OR_NEWER
        private void OnDestroy()
        {
            lifetimeCancellationTokenSource.Cancel();
        }
#endif

        public async Task BufferCallback(float[] bufferCallback)
        {
            foreach (var sample in bufferCallback)
            {
                audioBuffer.Enqueue(sample);
            }

            await Task.Yield();
        }

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

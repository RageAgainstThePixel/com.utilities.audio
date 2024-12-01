// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Utilities.Async;

namespace Utilities.Audio
{
    public class PCMEncoder : IEncoder
    {
        private const int HammingWindowFilterLength = 32;

        internal static readonly ISampleProvider DefaultSampleProvider = new UnitySampleProvider();

        /// <summary>
        /// Encodes the <see cref="samples"/> to raw pcm bytes.
        /// </summary>
        /// <param name="samples">Raw sample data</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="trim">Optional, trim the silence from the data.</param>
        /// <param name="silenceThreshold">Optional, silence threshold to use for trimming operations.</param>
        /// <param name="inputSampleRate"></param>
        /// <param name="outputSampleRate"></param>
        /// <returns>Byte array PCM data.</returns>
        [Preserve]
        public static byte[] Encode(float[] samples, PCMFormatSize size = PCMFormatSize.SixteenBit, bool trim = false, float silenceThreshold = 0.001f, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            if (inputSampleRate.HasValue && outputSampleRate.HasValue)
            {
                samples = Resample(samples, null, inputSampleRate.Value, outputSampleRate.Value);
            }
            else if (inputSampleRate.HasValue || outputSampleRate.HasValue)
            {
                throw new ArgumentException("Both input and output sample rates must be specified to resample the audio data.");
            }

            var sampleCount = samples.Length;
            var start = 0;
            var end = sampleCount;
            var length = sampleCount;

            if (trim)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        start = Math.Max(i - 1, 0);
                        break;
                    }
                }

                for (var i = sampleCount - 1; i >= start; i--)
                {
                    if (Math.Abs(samples[i]) > silenceThreshold)
                    {
                        end = i + 1;
                        break;
                    }
                }

                length = end - start;

                if (length <= 0)
                {
                    throw new InvalidOperationException("Trimming operation failed due to incorrect silence detection.");
                }
            }

            var offset = (int)size;
            var pcmData = new byte[length * offset];

            // Ensuring samples are within [-1,1] range
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = Math.Max(-1f, Math.Min(1f, samples[i]));
            }

            // Convert and write data
            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)Math.Max(Math.Min(Math.Round(value * 127 + 128), 255), 0);
                        pcmData[i - start] = (byte)sample;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (short)(value * short.MaxValue);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)(value * ((1 << 23) - 1));
                        sample = Math.Min(Math.Max(sample, -(1 << 23)), (1 << 23) - 1);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                        pcmData[stride + 2] = (byte)((sample >> 16) & byte.MaxValue);
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = start; i < end; i++)
                    {
                        var value = samples[i];
                        var sample = (int)(value * int.MaxValue);
                        var stride = (i - start) * offset;
                        pcmData[stride] = (byte)(sample & byte.MaxValue);
                        pcmData[stride + 1] = (byte)((sample >> 8) & byte.MaxValue);
                        pcmData[stride + 2] = (byte)((sample >> 16) & byte.MaxValue);
                        pcmData[stride + 3] = (byte)((sample >> 24) & byte.MaxValue);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }

            return pcmData;
        }

        /// <summary>
        /// Decodes the raw PCM byte data to samples.
        /// </summary>
        /// <param name="pcmData">PCM data to decode.</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="inputSampleRate"></param>
        /// <param name="outputSampleRate"></param>
        /// <returns>Float array of samples.</returns>
        [Preserve]
        public static float[] Decode(byte[] pcmData, PCMFormatSize size = PCMFormatSize.SixteenBit, int? inputSampleRate = null, int? outputSampleRate = null)
        {
            if (pcmData.Length % (int)size != 0)
            {
                throw new ArgumentException($"{nameof(pcmData)} length must be multiple of the specified {nameof(PCMFormatSize)}!", nameof(pcmData));
            }

            var sampleCount = pcmData.Length / (int)size;
            var samples = new float[sampleCount];
            var sampleIndex = 0;

            switch (size)
            {
                case PCMFormatSize.EightBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = pcmData[i];
                        var normalized = (sample - 128f) / 127f; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.SixteenBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = (short)((pcmData[i * 2 + 1] << 8) | pcmData[i * 2]);
                        var normalized = sample / (float)short.MaxValue; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.TwentyFourBit:
                    for (var i = 0; i < sampleCount; i++)
                    {
                        var sample = (pcmData[i * 3] << 0) | (pcmData[i * 3 + 1] << 8) | (pcmData[i * 3 + 2] << 16);
                        sample = (sample & 0x800000) != 0 ? sample | unchecked((int)0xff000000) : sample & 0x00ffffff;
                        var normalized = sample / (float)(1 << 23); // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                case PCMFormatSize.ThirtyTwoBit:
                    for (var i = 0; i < pcmData.Length; i += 4)
                    {
                        var sample = (pcmData[i + 3] << 24) | (pcmData[i + 2] << 16) | (pcmData[i + 1] << 8) | pcmData[i];
                        var normalized = sample / (float)int.MaxValue; // Normalize to [-1, 1] range
                        samples[sampleIndex] = normalized;
                        sampleIndex++;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }

            if (inputSampleRate.HasValue && outputSampleRate.HasValue)
            {
                samples = Resample(samples, null, inputSampleRate.Value, outputSampleRate.Value);
            }
            else if (inputSampleRate.HasValue || outputSampleRate.HasValue)
            {
                throw new ArgumentException("Both input and output sample rates must be specified to resample the audio data.");
            }

            return samples;
        }

        /// <summary>
        /// Resample the sample data to the specified sampling rate.
        /// </summary>
        /// <param name="samples">Samples to resample.</param>
        /// <param name="inputSamplingRate">The sampling rate of the samples provided.</param>
        /// <param name="outputSamplingRate">The target sampling rate to resample to.</param>
        /// <returns>Float array of samples at specified output sampling rate.</returns>
        [Preserve]
        [Obsolete("use overload with buffer input")]
        public static float[] Resample(float[] samples, int inputSamplingRate, int outputSamplingRate)
            => Resample(samples, null, inputSamplingRate, outputSamplingRate);

        /// <summary>
        /// Resample the sample data to the specified sampling rate.
        /// </summary>
        /// <param name="samples">Samples to resample.</param>
        /// <param name="buffer">The buffer to use for resampling.</param>
        /// <param name="inputSampleRate">The sampling rate of the samples provided.</param>
        /// <param name="outputSampleRate">The target sampling rate to resample to.</param>
        /// <returns>Float array of samples at specified output sampling rate.</returns>
        [Preserve]
        public static float[] Resample(float[] samples, float[] buffer, int inputSampleRate, int outputSampleRate)
        {
            if (inputSampleRate == outputSampleRate) { return samples; }

            var ratio = outputSampleRate / (double)inputSampleRate;
            var resampledLength = (int)Math.Round(samples.Length * ratio, MidpointRounding.ToEven);
            buffer ??= new float[resampledLength];

            if (buffer.Length != resampledLength)
            {
                buffer = new float[resampledLength];
            }

            for (var i = 0; i < resampledLength; i++)
            {
                var resampleIndex = Math.Round(i / ratio, MidpointRounding.ToEven);
                var leftIndex = (int)Math.Floor(resampleIndex);
                var rightIndex = leftIndex + 1;
                var fraction = resampleIndex - leftIndex;
                var leftSample = (leftIndex >= samples.Length ? samples[^1] : samples[leftIndex]) * (1 - fraction);
                var rightSample = (rightIndex >= samples.Length ? samples[^1] : samples[rightIndex]) * fraction;
                buffer[i] = (float)(leftSample + rightSample);
            }

            return buffer;
        }

        /// <inheritdoc />
        [Preserve]
        public async Task StreamRecordingAsync(ClipData clipData, Func<ReadOnlyMemory<byte>, Task> bufferCallback, CancellationToken cancellationToken, string callingMethodName = null)
        {
            if (callingMethodName != nameof(RecordingManager.StartRecordingStreamAsync))
            {
                throw new InvalidOperationException($"{nameof(StreamRecordingAsync)} can only be called from {nameof(RecordingManager.StartRecordingStreamAsync)} not {callingMethodName}");
            }

            RecordingManager.IsProcessing = true;

            try
            {
                await InternalStreamRecordAsync(clipData, null, bufferCallback, DefaultSampleProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case TaskCanceledException:
                    case OperationCanceledException:
                        // ignore
                        break;
                    default:
                        Debug.LogException(e);
                        break;
                }
            }
            finally
            {
                RecordingManager.IsProcessing = false;

                if (RecordingManager.EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] Finished processing...");
                }
            }
        }

        /// <inheritdoc />
        [Preserve]
        public async Task<Tuple<string, AudioClip>> StreamSaveToDiskAsync(ClipData clipData, string saveDirectory, Action<Tuple<string, AudioClip>> callback, CancellationToken cancellationToken, string callingMethodName = null)
        {
            if (callingMethodName != nameof(RecordingManager.StartRecordingAsync))
            {
                throw new InvalidOperationException($"{nameof(StreamSaveToDiskAsync)} can only be called from {nameof(RecordingManager.StartRecordingAsync)} not {callingMethodName}");
            }

            var outputPath = string.Empty;
            Tuple<string, AudioClip> result;
            RecordingManager.IsProcessing = true;

            try
            {
                Stream outStream;

                if (!string.IsNullOrWhiteSpace(saveDirectory))
                {
                    if (!Directory.Exists(saveDirectory))
                    {
                        Directory.CreateDirectory(saveDirectory);
                    }

                    outputPath = $"{saveDirectory}/{clipData.Name}.raw";

                    if (File.Exists(outputPath))
                    {
                        Debug.LogWarning($"[{nameof(RecordingManager)}] {outputPath} already exists, attempting to delete...");
                        File.Delete(outputPath);
                    }

                    outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                }
                else
                {
                    outStream = new MemoryStream();
                }

                var totalSampleCount = 0;
                var maxSampleLength = (clipData.MaxSamples ?? clipData.OutputSampleRate * RecordingManager.MaxRecordingLength) * clipData.Channels;
                var finalSamples = new float[maxSampleLength];
                var writer = new BinaryWriter(outStream);

                try
                {
                    try
                    {
                        async Task BufferCallback(ReadOnlyMemory<byte> buffer)
                        {
                            writer.Write(buffer.Span);
                            await Task.Yield();
                        }

                        (finalSamples, totalSampleCount) = await InternalStreamRecordAsync(clipData, finalSamples, BufferCallback, DefaultSampleProvider, cancellationToken).ConfigureAwait(true);
                    }
                    finally
                    {
                        if (RecordingManager.EnableDebug)
                        {
                            Debug.Log($"[{nameof(RecordingManager)}] Flush stream...");
                        }

                        writer.Flush();
                    }
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case TaskCanceledException:
                        case OperationCanceledException:
                            // ignore
                            break;
                        default:
                            Debug.LogException(e);
                            break;
                    }
                }
                finally
                {
                    if (RecordingManager.EnableDebug)
                    {
                        Debug.Log($"[{nameof(RecordingManager)}] Dispose stream...");
                    }

                    await writer.DisposeAsync().ConfigureAwait(false);
                    await outStream.DisposeAsync().ConfigureAwait(false);
                }

                if (RecordingManager.EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] Finalized file write. Copying recording into new AudioClip");
                }

                // Trim the final samples down into the recorded range.
                var microphoneData = new float[totalSampleCount * clipData.Channels];
                Array.Copy(finalSamples, microphoneData, microphoneData.Length);
                await Awaiters.UnityMainThread; // switch back to main thread to call unity apis
                // Create a new copy of the final recorded clip.
                var newClip = AudioClip.Create(clipData.Name, microphoneData.Length, clipData.Channels, clipData.OutputSampleRate, false);
                newClip.SetData(microphoneData, 0);
                result = new Tuple<string, AudioClip>(outputPath, newClip);
                callback?.Invoke(result);
            }
            finally
            {
                RecordingManager.IsProcessing = false;

                if (RecordingManager.EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] Finished processing...");
                }
            }
            return result;
        }

        internal static async Task<(float[], int)> InternalStreamRecordAsync(ClipData clipData, float[] finalSamples, Func<ReadOnlyMemory<byte>, Task> bufferCallback, ISampleProvider sampleProvider, CancellationToken cancellationToken)
        {
            try
            {
                int? maxSampleLength = null;

                if (finalSamples != null)
                {
                    // make sure that the final sample buffer matches the expected length
                    maxSampleLength = (clipData.MaxSamples ?? clipData.OutputSampleRate * RecordingManager.MaxRecordingLength) * clipData.Channels;

                    if (finalSamples.Length != maxSampleLength)
                    {
                        Debug.LogWarning($"[{nameof(RecordingManager)}] final sample buffer length does match expected length! creating new buffer...");
                        finalSamples = new float[maxSampleLength.Value];
                    }
                }

                var sampleCount = 0;
                var shouldStop = false;
                var lastMicrophonePosition = 0;
                var needsResample = clipData.InputSampleRate != clipData.OutputSampleRate;
                var ratio = needsResample ? clipData.OutputSampleRate / (double)clipData.InputSampleRate : 1d;
                var inputBufferSize = clipData.InputBufferSize;
                var sampleBuffer = new float[inputBufferSize];

                do
                {
                    await Awaiters.UnityMainThread; // ensure we're on main thread to call unity apis
                    var microphonePosition = sampleProvider.GetPosition(clipData.Device);

                    if (microphonePosition <= 0 && lastMicrophonePosition == 0)
                    {
                        if (RecordingManager.EnableDebug)
                        {
                            Debug.LogWarning($"[{nameof(RecordingManager)}] Microphone position is 0, skipping...");
                        }

                        // Skip this iteration if there aren't any samples
                        continue;
                    }

                    var isLooping = microphonePosition < lastMicrophonePosition;
                    int samplesToWrite;

                    if (isLooping)
                    {
                        // Microphone loopback detected.
                        samplesToWrite = inputBufferSize - lastMicrophonePosition;

                        if (RecordingManager.EnableDebug)
                        {
                            Debug.LogWarning($"[{nameof(RecordingManager)}] Microphone loopback detected! [{microphonePosition} < {lastMicrophonePosition}] samples to write: {samplesToWrite}");
                        }
                    }
                    else
                    {
                        // No loopback, process normally.
                        samplesToWrite = microphonePosition - lastMicrophonePosition;
                    }

                    if (samplesToWrite > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        sampleProvider.GetData(clipData.Clip, sampleBuffer);

                        var outputSamplesToWrite = needsResample
                            ? (int)(samplesToWrite * ratio)
                            : samplesToWrite;

                        for (var i = 0; i < outputSamplesToWrite; i++)
                        {
                            float value;

                            if (needsResample)
                            {
                                var resampleIndex = Math.Round(lastMicrophonePosition + i / ratio, MidpointRounding.ToEven);
                                var leftIndex = (int)Math.Floor(resampleIndex) % inputBufferSize; // Wrap around index.

                                // Simple Linear Interpolation
                                var rightIndex = (leftIndex + 1) % inputBufferSize; // Wrap around index.
                                var fraction = resampleIndex - leftIndex;
                                var leftSample = sampleBuffer[leftIndex] * fraction;
                                var rightSample = sampleBuffer[rightIndex] * (1 - fraction);
                                value = (float)(leftSample + rightSample);
                            }
                            else
                            {
                                var bufferIndex = (lastMicrophonePosition + i) % inputBufferSize; // Wrap around index.
                                value = sampleBuffer[bufferIndex];
                            }

                            try
                            {
                                await bufferCallback.Invoke(ToReadOnlyMemory(NormalizeSample(value))).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(new Exception($"[{nameof(PCMEncoder)}] error occurred when buffering audio", e));
                            }

                            if (finalSamples is { Length: > 0 })
                            {
                                finalSamples[sampleCount * clipData.Channels + i] = value;
                            }
                        }

                        lastMicrophonePosition = microphonePosition;
                        sampleCount += outputSamplesToWrite;

                        if (RecordingManager.EnableDebug)
                        {
                            Debug.Log($"[{nameof(RecordingManager)}] State: {nameof(RecordingManager.IsRecording)}? {RecordingManager.IsRecording} | Wrote {samplesToWrite} samples | last mic pos: {lastMicrophonePosition} | total samples: {sampleCount} | isCancelled? {cancellationToken.IsCancellationRequested}");
                        }
                    }

                    if (cancellationToken.IsCancellationRequested || sampleCount >= maxSampleLength)
                    {
                        if (RecordingManager.EnableDebug)
                        {
                            Debug.Log("Breaking internal record loop!");
                        }

                        shouldStop = true;
                    }
                } while (!shouldStop);
                return (finalSamples, sampleCount);
            }
            finally
            {
                RecordingManager.IsRecording = false;
                await Awaiters.UnityMainThread; // ensure we're on main thread to call unity apis
                sampleProvider.End(clipData.Device);

                if (RecordingManager.EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] Recording stopped");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double SinC(double x)
            => x == 0d ? 1d : Math.Sin(Math.PI * x) / (Math.PI * x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double HammingWindow(int n, int filterLength)
            => 0.54d - 0.46d * Math.Cos(2 * Math.PI * n / (filterLength - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short NormalizeSample(float value)
            => (short)(Math.Max(-1f, Math.Min(1f, value)) * short.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlyMemory<byte> ToReadOnlyMemory(short sample)
            => new(new[]
            {
                (byte)(sample & byte.MaxValue),
                (byte)(sample >> 8 & byte.MaxValue)
            });
    }
}

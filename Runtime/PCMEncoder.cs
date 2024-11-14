// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Utilities.Async;

namespace Utilities.Audio
{
    public class PCMEncoder : IEncoder
    {
        /// <summary>
        /// Encodes the <see cref="samples"/> to raw pcm bytes.
        /// </summary>
        /// <param name="samples">Raw sample data</param>
        /// <param name="size">Size of PCM sample data.</param>
        /// <param name="trim">Optional, trim the silence from the data.</param>
        /// <param name="silenceThreshold">Optional, silence threshold to use for trimming operations.</param>
        /// <returns>Byte array PCM data.</returns>
        [Preserve]
        public static byte[] Encode(float[] samples, PCMFormatSize size = PCMFormatSize.SixteenBit, bool trim = false, float silenceThreshold = 0.001f)
        {
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
        [Preserve]
        public static float[] Decode(byte[] pcmData, PCMFormatSize size = PCMFormatSize.SixteenBit)
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
        public static float[] Resample(float[] samples, int inputSamplingRate, int outputSamplingRate)
        {
            var ratio = (double)outputSamplingRate / inputSamplingRate;
            var outputLength = (int)(samples.Length * ratio);
            var result = new float[outputLength];

            for (var i = 0; i < outputLength; i++)
            {
                var position = i / ratio;
                var leftIndex = (int)Math.Floor(position);
                var rightIndex = leftIndex + 1;
                var fraction = position - leftIndex;

                if (rightIndex >= samples.Length)
                {
                    result[i] = samples[leftIndex];
                }
                else
                {
                    result[i] = (float)(samples[leftIndex] * (1 - fraction) + samples[rightIndex] * fraction);
                }
            }

            return result;
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
                await InternalStreamRecordAsync(clipData, null, bufferCallback, cancellationToken).ConfigureAwait(false);
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
            RecordingManager.IsProcessing = true;
            Tuple<string, AudioClip> result = null;

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
                var finalSamples = new float[clipData.MaxSamples ?? clipData.SampleRate * RecordingManager.MaxRecordingLength];
                var writer = new BinaryWriter(outStream);

                try
                {
                    try
                    {
                        (finalSamples, totalSampleCount) = await InternalStreamRecordAsync(clipData, finalSamples, async buffer =>
                        {
                            writer.Write(buffer.Span);
                            await Task.Yield();
                        }, cancellationToken).ConfigureAwait(true);
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
                var newClip = AudioClip.Create(clipData.Name, microphoneData.Length, clipData.Channels, clipData.SampleRate, false);
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

        private static async Task<(float[], int)> InternalStreamRecordAsync(ClipData clipData, float[] finalSamples, Func<ReadOnlyMemory<byte>, Task> bufferCallback, CancellationToken cancellationToken)
        {
            try
            {
                var sampleCount = 0;
                var shouldStop = false;
                var lastMicrophonePosition = 0;
                var sampleBuffer = new float[clipData.BufferSize];
                do
                {
                    await Awaiters.UnityMainThread; // ensure we're on main thread to call unity apis
                    var microphonePosition = Microphone.GetPosition(clipData.Device);

                    if (microphonePosition <= 0 && lastMicrophonePosition == 0)
                    {
                        // Skip this iteration if there's no new data
                        // wait for next update
                        continue;
                    }

                    var isLooping = microphonePosition < lastMicrophonePosition;
                    int samplesToWrite;

                    if (isLooping)
                    {
                        // Microphone loopback detected.
                        samplesToWrite = clipData.BufferSize - lastMicrophonePosition;

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

                    if (samplesToWrite > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        clipData.Clip.GetData(sampleBuffer, 0);

                        for (var i = 0; i < samplesToWrite; i++)
                        {
                            var bufferIndex = (lastMicrophonePosition + i) % clipData.BufferSize; // Wrap around index.
                            var value = sampleBuffer[bufferIndex];
                            var sample = (short)(Math.Max(-1f, Math.Min(1f, value)) * short.MaxValue);
                            var sampleData = new ReadOnlyMemory<byte>(new[]
                            {
                                (byte)(sample & byte.MaxValue),
                                (byte)(sample >> 8 & byte.MaxValue)
                            });

                            try
                            {
                                await bufferCallback.Invoke(sampleData).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(new Exception($"[{nameof(PCMEncoder)}] error occurred when buffering audio", e));
                            }

                            if (finalSamples is { Length: > 0 })
                            {
                                finalSamples[sampleCount * clipData.Channels + i] = sampleBuffer[bufferIndex];
                            }
                        }

                        lastMicrophonePosition = microphonePosition;
                        sampleCount += samplesToWrite;

                        if (RecordingManager.EnableDebug)
                        {
                            Debug.Log($"[{nameof(RecordingManager)}] State: {nameof(RecordingManager.IsRecording)}? {RecordingManager.IsRecording} | Wrote {samplesToWrite} samples | last mic pos: {lastMicrophonePosition} | total samples: {sampleCount} | isCancelled? {cancellationToken.IsCancellationRequested}");
                        }
                    }

                    if (clipData.MaxSamples.HasValue && sampleCount >= clipData.MaxSamples || cancellationToken.IsCancellationRequested)
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
                Microphone.End(clipData.Device);

                if (RecordingManager.EnableDebug)
                {
                    Debug.Log($"[{nameof(RecordingManager)}] Recording stopped");
                }
            }
        }
    }
}

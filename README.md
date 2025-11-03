# com.utilities.audio

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.audio?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.audio/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.audio)](https://openupm.com/packages/com.utilities.audio/)

A simple package for audio extensions and utilities in the [Unity](https://unity.com/) Game Engine.

## Installing

Requires Unity 2021.3 LTS or higher.

The recommended installation method is though the unity package manager and [OpenUPM](https://openupm.com/packages/com.utilities.audio).

### Via Unity Package Manager and OpenUPM

#### Terminal

```terminal
openupm add com.utilities.audio
```

#### Manual

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](Utilities.Audio/Packages/com.utilities.audio/Documentation~/images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - Name: `OpenUPM`
  - URL: `https://package.openupm.com`
  - Scope(s):
    - `com.utilities`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Audio` package

### Via Unity Package Manager and Git url

> [!WARNING]
> This repo has dependencies on other repositories! You are responsible for adding these on your own.

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.audio.git#upm`
  - [com.utilities.async](https://github.com/RageAgainstThePixel/com.utilities.async)
  - [com.utilities.extensions](https://github.com/RageAgainstThePixel/com.utilities.extensions)

## Documentation

On its own this package doesn't do too much but provide base functionality for recording audio in the Unity Editor and during runtime. Instead, use the [encoder packages](#encoder-packages) to fully utilize this package and its contents.

### Table of Contents

- [Encoder Packages](#encoder-packages)
- [Recording Manager](#recording-manager)
  - [Steam Save To Disk](#start-recording-while-streaming-to-disk)
  - [Stream Callbacks](#stream-callbacks)
    - [Raw Buffer Callback](#raw-buffer-callback)
    - [Sample Callback](#sample-callback)
- [Recording Behaviour](#recording-behaviour)
- [Audio Streaming Behaviour](#audio-streaming-behaviour)
- [Audio Reactive Example](#audio-reactive-example) :new:
- [Audio Clip Extensions](#audio-clip-extensions)
  - [Encode PCM](#encode-pcm-to-bytes)
  - [Decode PCM](#decode-pcm-from-bytes)
- [IEncoder](#iencoder)

## Encoder Packages

- [Ogg Encoder](https://github.com/RageAgainstThePixel/com.utilities.encoder.ogg)
- [Wav Encoder](https://github.com/RageAgainstThePixel/com.utilities.encoder.wav)

## Recording Manager

This class is meant to be used anywhere you want to be able to record audio. You can use one of the [encoder packages](#encoder-packages) to be able to record and encode to the specific format other than PCM.

A perfect example implementation on how to use the `RecordingManager` is in the `AbstractRecordingBehaviour<TEncoder>` class.

### Start Recording while streaming to disk

```csharp
var (savedPath, recordedClip) = await RecordingManager.StartRecordingAsync<PCMEncoder>("my recording", "directory/to/save");
```

### Stream Callbacks

You can provide a callback to process the captured audio samples or buffer as they are recorded.

#### Raw Buffer Callback

Write the raw encoded buffer to a `MemoryStream` that can be saved to disk or processed later.

```csharp
using var stream = new MemoryStream();
await RecordingManager.StartRecordingStreamAsync<PCMEncoder>(bufferCallback => stream.WriteAsync(bufferCallback));
```

#### Sample Callback

Forward the samples to a `StreamAudioSource` component for realtime audio streaming or processing.

```csharp
RecordingManager.StartRecordingStream<PCMEncoder>(
    (samples, count) => streamAudioSource.SampleCallback(samples, count),
    recordingSampleRate,
    destroyCancellationToken);
```

## Recording Behaviour

A basic `PCMRecordingBehaviour` is included in this package to enable basic recording to any project. Simply add this component to any GameObject in your scene. This class inherits from `AbstractRecordingBehaviour<TEncoder>`.

`AbstractRecordingBehaviour<TEncoder>` is really meant to be a good baseline example of how to use the `RecordingManager`. This abstract class is implemented in each of the [encoder packages](#encoder-packages) for simplicity and ease of use. You can use this class as an example of how to implement your own recording behaviours.

## Audio Streaming Behaviour

A `StreamAudioSource` component is included in this package to enable basic audio streaming to any project. Simply add this component to any GameObject in your scene.

This component was designed to streamline platform support for WebGL since it doesn't support `OnAudioFilterRead` API. On platforms that support it, it will use the `OnAudioFilterRead` API to stream audio data. On WebGL, it will stream the data directly to an instanced `AudioContext` object for you.

> [!NOTE]
> Volume control is provided by the `AudioSource` component, so updating the volume on the AudioSource as normal, will also update the volume of the `AudioContext` in WebGL.

## Audio Reactive Example

The samples contain an example `AudioReactiveBehaviour` implementation that uses the `StreamAudioSource` component. This example uses the audio data from the streaming source to create a simple scaling effect on a GameObject.

This example uses `OnAudioFilterRead` to get the audio data from the `StreamAudioSource` component and then uses that data to scale the GameObject.

> [!WARNING]
> `OnAudioFilterRead` is not supported in WebGL, so this example will only work in the editor and on platforms that support it.

## Audio Clip Extensions

Provides extensions to encode `AudioClip`s to PCM encoded bytes.
Supports 8, 16, 24, and 32 bit sample sizes.

### Encode PCM to bytes

> [!WARNING]
> The resulting `NativeArray<byte>` must be disposed of manually to avoid memory leaks, you can adjust the allocator type by passing in an optional parameter.

```csharp
// Encodes the audioClip to PCM byte data.
using NativeArray<byte> pcmBytes = audioClip.EncodeToPCM();
```

### Decode PCM from bytes

Decodes the raw PCM byte data and sets it to the `AudioClip`.
Can be either `NativeArray<byte>` or `byte[]` types.

> [!Note]
> Unity 6+, it is recommended to use `NativeArray<byte>` for better performance.

```csharp
audioClip.DecodeFromPCM(pcmBytes);
```

## IEncoder

This package also includes an `IEncoder` interface to allow for custom encoders to be implemented. This interface is used in the [encoder packages](#encoder-packages) to allow for custom encoders to be implemented.

The interface contains the following methods:

- **StreamRecordingAsync**: Streams audio microphone recording input to memory, with bufferCallbacks for each sample.
- **StreamSaveToDiskAsync**: Streams audio microphone recording input to disk, with callback when recording has been saved to disk.

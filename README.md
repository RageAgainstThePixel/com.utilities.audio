# com.utilities.audio

[![Discord](https://img.shields.io/discord/855294214065487932.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/xQgMW9ufN4) [![openupm](https://img.shields.io/npm/v/com.utilities.audio?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.utilities.audio/) [![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.utilities.audio)](https://openupm.com/packages/com.utilities.audio/)

A simple package for audio extensions and utilities in the [Unity](https://unity.com/) Game Engine.

## Installing

### Via Unity Package Manager and OpenUPM

- Open your Unity project settings
- Select the `Package Manager`
![scoped-registries](Utilities.Audio/Packages/com.utilities.audio/Documentation~/images/package-manager-scopes.png)
- Add the OpenUPM package registry:
  - `Name: OpenUPM`
  - `URL: https://package.openupm.com`
  - `Scope(s):`
    - `com.utilities.audio`
- Open the Unity Package Manager window
- Change the Registry from Unity to `My Registries`
- Add the `Utilities.Audio` package

### Via Unity Package Manager and Git url

- Open your Unity Package Manager
- Add package from git url: `https://github.com/RageAgainstThePixel/com.utilities.audio.git#upm`
  > Note: this repo has dependencies on other repositories! You are responsible for adding these on your own.
  - [com.utilities.async](https://github.com/RageAgainstThePixel/com.utilities.async)

## Documentation

On its own this package doesn't do too much but provide base functionality for recording audio in the Unity Editor and during runtime. Instead, use the [encoder packages](#encoder-packages) to fully utilize this package and its contents.

### Table of Contents

- [Encoder Packages](#encoder-packages)
- [Recording Manager](#recording-manager)
- [Recording Behaviour](#recording-behaviour)
- [Audio Clip Extensions](#audio-clip-extensions)

## Encoder Packages

- [Ogg Encoder](https://github.com/RageAgainstThePixel/com.utilities.encoder.ogg)
- [Wav Encoder](https://github.com/RageAgainstThePixel/com.utilities.encoder.wav)

## Recording Manager

This class is meant to be used anywhere you want to be able to record audio. You will need to have one of the [encoder packages](#encoder-packages) to be able to record and encode to the specific format.

A perfect example implementation on how to use this is in the `AbstractRecordingBehaviour` class.

## Recording Behaviour

A basic `AbstractRecordingBehaviour` is included in this package to make it very simple to add recording to any GameObject in the scene. This class is really meant to be a good baseline example of how to use the `RecordingManager`. This abstract class is implemented in each of the encoder packages for simplicity and ease of use.

## Audio Clip Extensions

Provides extensions to encode `AudioClip`s to PCM encoded bytes.
Supports 8, 16, 24, and 32 bit sample sizes.

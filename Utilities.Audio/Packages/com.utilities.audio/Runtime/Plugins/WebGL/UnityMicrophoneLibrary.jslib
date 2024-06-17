var UnityMicrophoneLibrary = {
  /**
   * Array of instanced Microphone objects.
   */
  $microphoneDevices: [],
  /**
   * Initializes the Microphone context. Will alert the user if the browser does not support recording.
   * Sets up an interval to check the audio context state and resume it if it is suspended or interrupted.
   * @param {number} onEnumerateDevicesPtr The pointer to the enumerate devices callback.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if the browser does not support recording.
   */
  Microphone_Init: function (onEnumerateDevicesPtr) {
    try {
      if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices || !navigator.mediaDevices.getUserMedia) {
        alert('UnityMicrophoneLibrary is not supported in this browser!');
        return 2;
      }

      navigator.mediaDevices.getUserMedia({ audio: true }).then(_ => {
        console.log("UnityMicrophoneLibrary permissions granted!");
        queryAudioDevices(onEnumerateDevicesPtr);

        navigator.mediaDevices.ondevicechange = (_) => {
          queryAudioDevices(onEnumerateDevicesPtr);
        };
      }).catch(error => {
        console.error(error);
      });

      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  /**
   * Gets the number of devices available.
   * @returns {number} The number of devices available.
   */
  Microphone_GetNumberOfDevices: function () {
    try {
      return microphoneDevices.length;
    } catch (error) {
      console.error(error);
      return 0;
    }
  },
  /**
   * Gets the name of the device at the specified index.
   * @param {number} index The index of the device to get the name for.
   * @returns {string} The name of the device at the specified index.
   */
  Microphone_GetDeviceName: function (index) {
    try {
      if (!microphoneDevices) {
        return null;
      }

      if (index >= 0 && index < microphoneDevices.length) {
        var deviceName = microphoneDevices[index].name;
        var length = lengthBytesUTF8(deviceName) + 1;
        var buffer = _malloc(length);
        stringToUTF8(deviceName, buffer, length);
        return buffer;
      }

      return null;
    } catch (error) {
      console.error(error);
      return null;
    }
  },
  /**
   * Starts recording from the specified device.
   * @param {string} deviceName The name of the device. If string is null or empty, the default device is used.
   * @param {boolean} loop Indicates whether the recording should continue recording if length is reached, and wrap around and record from the beginning of the buffer.
   * @param {number} length The length of the recording in seconds.
   * @param {number} frequency The sample rate of the recording.
   * @param {number} onBufferReadPtr The pointer to the buffer read callback.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if the browser does not support recording.
  */
  Microphone_StartRecording: function (deviceName, loop, length, frequency, onBufferReadPtr) {
    console.log("Microphone_StartRecording");
    try {
      if (!navigator.mediaDevices.getUserMedia) {
        console.error('UnityMicrophoneLibrary: not supported in this browser!');
        return 2;
      }

      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      microphone.position = 0;
      microphone.loop = loop;
      microphone.pcmBuffer = new Float32Array(length * frequency);
      microphone.onBufferReadCallback = onBufferReadPtr;

      if (frequency <= 0) {
        frequency = microphone.maxFrequency;
      }

      var constraints = {
        audio: {
          deviceId: microphone.device.deviceId ? { exact: microphone.device.deviceId } : undefined,
          sampleRate: { ideal: frequency },
        }
      };

      navigator.mediaDevices.getUserMedia(constraints).then(stream => {
        const audioContext = new AudioContext({ sampleRate: frequency });
        const source = audioContext.createMediaStreamSource(stream);
        const processor = audioContext.createScriptProcessor();

        processor.onaudioprocess = (event) => {
          if (!microphone.pcmBuffer || microphone.pcmBuffer.length <= 0) {
            throw new Error("UnityMicrophoneLibrary: pcmBuffer not initialized!");
          }

          const data = event.inputBuffer.getChannelData(0); // unity only supports mono

          for (var i = 0; i < data.length; i++) {
            microphone.pcmBuffer[microphone.position] = data[i];
            microphone.position++;

            if (microphone.position >= microphone.pcmBuffer.length) {
              if (microphone.loop) {
                microphone.position = 0;
              } else {
                break;
              }
            }
          }

          console.log(`Microphone position: ${microphone.position} | data read: ${data.length}`);
          var audioBuffer = Module._malloc(microphone.pcmBuffer.length * 4);
          console.log(`Allocated buffer at: ${audioBuffer} with length: ${microphone.pcmBuffer.length}`);
          try {
            writeArrayToMemory(microphone.pcmBuffer, audioBuffer);
            Module.dynCall_vii(microphone.onBufferReadCallback, audioBuffer, audioBuffer.length);
          } finally {
            Module._free(audioBuffer);
          }
        };

        source.connect(processor);
        processor.connect(audioContext.destination);

        microphone.processor = processor;
        microphone.source = source;
        microphone.audioContext = audioContext;
        microphone.stream = stream;
      }).catch(error => {
        console.error(error);
      });

      microphone.isRecording = true;
      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  /**
   * Stops recording from the specified device.
   * @param {string} deviceName The name of the device to stop recording from.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if no recording is in progress.
   */
  Microphone_StopRecording: function (deviceName) {
    console.log("Microphone_StopRecording");
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));

      if (!microphone.isRecording) {
        console.warn("UnityMicrophoneLibrary: no recording in progress")
        return 2;
      }

      try {
        microphone.processor.disconnect();
        microphone.source.disconnect();
        microphone.audioContext.close();
        microphone.stream.getTracks().forEach(track => track.stop());
      } finally {
        microphone.isRecording = false;
        microphone.onBufferReadCallback = null;
        microphone.audioContext = null;
        microphone.pcmBuffer = null;
        microphone.processor = null;
        microphone.loop = undefined;
        microphone.source = null;
        microphone.stream = null;
      }

      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  /**
   * Checks if the specified device is recording.
   * @param {string} deviceName The name of the device to check if it is recording.
   * @returns {boolean} Whether the specified device is recording.
   */
  Microphone_IsRecording: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      return microphone.isRecording;
    } catch (error) {
      console.error(error);
      return false;
    }
  },
  /**
   * Gets the maximum frequency of the microphone.
   * @param {string} deviceName The name of the device to get the maximum frequency for.
   * @returns {number} The maximum frequency of the microphone.
   */
  Microphone_GetMaxFrequency: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      return microphone.maxFrequency;
    } catch (error) {
      console.error(error);
      return -1;
    }
  },
  /**
   * Gets the minimum frequency of the microphone.
   * @param {string} deviceName The name of the device to get the minimum frequency for.
   * @returns {number} The minimum frequency of the microphone.
   */
  Microphone_GetMinFrequency: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      return microphone.minFrequency;
    } catch (error) {
      console.error(error);
      return -1;
    }
  },
  /**
   * Gets the position of the microphone.
   * @param {string} deviceName The name of the device to get the position for.
   * @returns {number} The position of the microphone.
   */
  Microphone_GetPosition: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      return microphone.position;
    } catch (error) {
      console.error(error);
      return -1;
    }
  }
}

autoAddDeps(UnityMicrophoneLibrary, '$microphoneDevices');
mergeInto(LibraryManager.library, UnityMicrophoneLibrary);

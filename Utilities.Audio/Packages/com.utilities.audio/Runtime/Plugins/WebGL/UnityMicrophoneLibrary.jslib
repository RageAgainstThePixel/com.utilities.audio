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

      // Thanks to De-Panther for the following code
      // Checks if specific dynCall functions exist,
      // if not, it will create them using the getWasmTableEntry function.
      // https://discussions.unity.com/t/makedyncall-replacing-dyncall-in-unity-6/1543088
      Module.dynCall_v = Module.dynCall_v || function (cb) {
        return getWasmTableEntry(cb)();
      };
      Module.dynCall_vi = Module.dynCall_vi || function (cb, arg1) {
        return getWasmTableEntry(cb)(arg1);
      };

      navigator.mediaDevices.getUserMedia({ audio: true }).then(_ => {
        // console.log("UnityMicrophoneLibrary permissions granted!");
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
   * @param {number} audioBufferPtr The pointer to the audio buffer.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if the browser does not support recording.
  */
  Microphone_StartRecording: function (deviceName, loop, length, frequency, onBufferReadPtr, audioBufferPtr) {
    try {
      if (!navigator.mediaDevices.getUserMedia) {
        console.error('UnityMicrophoneLibrary: not supported in this browser!');
        return 2;
      }

      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));
      microphone.position = 0;
      microphone.loop = loop;
      microphone.audioBufferPtr = audioBufferPtr;
      microphone.onBufferReadPtr = onBufferReadPtr;
      microphone.samples = length * frequency;

      if (frequency <= 0) {
        frequency = microphone.maxFrequency;
      }

      var constraints = {
        audio: {
          deviceId: microphone.device.deviceId ? { exact: microphone.device.deviceId } : undefined,
          sampleRate: frequency,
        }
      };

      navigator.mediaDevices.getUserMedia(constraints).then(stream => {
        const audioContext = new AudioContext({ sampleRate: frequency });
        const source = audioContext.createMediaStreamSource(stream);
        const processor = audioContext.createScriptProcessor(4096, 1, 1);

        processor.onaudioprocess = (event) => {
          if (!microphone.isRecording || !microphone.audioBufferPtr || !microphone.onBufferReadPtr) {
            return; // recording was stopped, or buffer is not ready
          }

          const data = event.inputBuffer.getChannelData(0); // unity only supports mono

          for (var i = 0; i < data.length; i++) {
            Module.HEAPF32[microphone.audioBufferPtr / Float32Array.BYTES_PER_ELEMENT + microphone.position] = data[i];
            microphone.position++;

            if (microphone.position >= microphone.samples) {
              if (microphone.loop) {
                microphone.position = 0;
              } else {
                microphone.position = microphone.samples - 1;
                break;
              }
            }
          }

          Module.dynCall_vi(microphone.onBufferReadPtr, microphone.position);
        };

        source.connect(processor);
        processor.connect(audioContext.destination);

        microphone.processor = processor;
        microphone.source = source;
        microphone.audioContext = audioContext;
        microphone.stream = stream;
      }).catch(error => {
        console.error(error);
        this.Microphone_StopRecording(deviceName);
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
    try {
      var microphone = getMicrophoneDevice(UTF8ToString(deviceName));

      if (!microphone.isRecording) {
        console.warn("UnityMicrophoneLibrary: no recording in progress");
        return 2;
      }

      try {
        microphone.processor.disconnect();
        microphone.source.disconnect();
        microphone.audioContext.close();
        microphone.stream.getTracks().forEach(track => track.stop());
      } finally {
        microphone.isRecording = false;
        microphone.audioBufferPtr = null;
        microphone.onBufferReadPtr = null;
        microphone.audioContext = null;
        microphone.processor = null;
        microphone.loop = null;
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
  }
}

autoAddDeps(UnityMicrophoneLibrary, '$microphoneDevices');
mergeInto(LibraryManager.library, UnityMicrophoneLibrary);

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
   * @param {number} frequency The sample rate of the recording.
   * @param {number} onBufferReadPtr The pointer to the buffer read callback.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if the browser does not support recording.
  */
  Microphone_StartRecording: function (deviceName, loop, frequency, onBufferReadPtr) {
    console.log("Microphone_StartRecording");
    try {
      if (!navigator.mediaDevices.getUserMedia) {
        console.error('UnityMicrophoneLibrary: not supported in this browser!');
        return 2;
      }

      var microphone = getMicrophoneDevice(deviceName);

      microphone.position = 0;

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
          const data = event.inputBuffer.getChannelData(0);
          var buffer = Module._malloc(data.length * data.BYTES_PER_ELEMENT);
          writeArrayToMemory(data, buffer);

          try {
            Module.dynCall_vii(onBufferReadPtr, buffer, data.length);
          } finally {
            Module._free(buffer);
          }
        };

        source.connect(processor);
        processor.connect(audioContext.destination);

        microphone.processor = processor;
        microphone.source = source;
        microphone.audioContext = audioContext;
        microphone.stream = stream;

        microphone.isRecording = true;
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
   * Stops recording from the specified device.
   * @param {string} deviceName The name of the device to stop recording from.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if no recording is in progress.
   */
  Microphone_StopRecording: function (deviceName) {
    console.log("Microphone_StopRecording");
    try {
      var microphone = getMicrophoneDevice(deviceName);

      if (!microphone.isRecording) {
        console.warn("UnityMicrophoneLibrary: no recording in progress")
        return 2;
      }

      try {
        microphone.processor.disconnect();
        microphone.processor = null;
        microphone.source.disconnect();
        microphone.source = null;
        microphone.audioContext.close();
        microphone.audioContext = null;
        microphone.stream.getTracks().forEach(track => track.stop());
        microphone.stream = null;
      } finally {
        microphone.isRecording = false;
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
      var microphone = getMicrophoneDevice(deviceName);
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
      var microphone = getMicrophoneDevice(deviceName);
      return microphone.maxFrequency;
    } catch (error) {
      console.error(error);
      return 0;
    }
  },
  /**
   * Gets the minimum frequency of the microphone.
   * @param {string} deviceName The name of the device to get the minimum frequency for.
   * @returns {number} The minimum frequency of the microphone.
   */
  Microphone_GetMinFrequency: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(deviceName);
      return microphone.minFrequency;
    } catch (error) {
      console.error(error);
      return 0;
    }
  },
  /**
   * Gets the position of the microphone.
   * @param {string} deviceName The name of the device to get the position for.
   * @returns {number} The position of the microphone.
   */
  Microphone_GetPosition: function (deviceName) {
    try {
      var microphone = getMicrophoneDevice(deviceName);
      return microphone.position;
    } catch (error) {
      console.error(error);
      return 0;
    }
  }
}

autoAddDeps(UnityMicrophoneLibrary, '$microphoneDevices');
mergeInto(LibraryManager.library, UnityMicrophoneLibrary);

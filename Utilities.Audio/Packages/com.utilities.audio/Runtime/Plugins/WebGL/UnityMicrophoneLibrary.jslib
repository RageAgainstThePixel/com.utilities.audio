// Licensed under the MIT License. See LICENSE in the project root for license information.

var UnityMicrophoneLibrary = {
  /**
   * Array of instanced Microphone objects.
   */
  $microphoneDevices: [],
  /**
   * The active microphone device.
   */
  $activeMicrophone: null,
  /**
   * Initializes the Microphone context. Will alert the user if the browser does not support recording.
   * Sets up an interval to check the audio context state and resume it if it is suspended or interrupted.
   * @param {number} onEnumerateDevicesPtr The pointer to the onEnumerateDevices function callback.
   * @param {number} onPermissionGrantedPtr The pointer to the permission granted callback.
   * @param {number} onPermissionDeniedPtr The pointer to the permission denied callback.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred, 2 if the browser does not support recording.
   */
  Microphone_Init: function (onEnumerateDevicesPtr, onPermissionGrantedPtr, onPermissionDeniedPtr) {
    try {
      if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices || !navigator.mediaDevices.getUserMedia) {
        alert('UnityMicrophoneLibrary is not supported in this browser!');
        return 2;
      }
      navigator.mediaDevices.getUserMedia({ audio: true })
        .then(_ => {
          // console.log("UnityMicrophoneLibrary permissions granted!");
          queryAudioDevices(onEnumerateDevicesPtr);
          if (!navigator.mediaDevices.ondevicechange) {
            navigator.mediaDevices.ondevicechange = (_) => {
              queryAudioDevices(onEnumerateDevicesPtr);
            };
          }
          Module.dynCall_v(onPermissionGrantedPtr);
        }, reason => {
          console.error(`UnityMicrophoneLibrary: permissions denied! ${reason}`);
          Module.dynCall_v(onPermissionDeniedPtr);
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
      activeMicrophone = microphone;
      activeMicrophone.position = 0;
      activeMicrophone.loop = loop;
      activeMicrophone.audioBufferPtr = audioBufferPtr;
      activeMicrophone.onBufferReadPtr = onBufferReadPtr;
      activeMicrophone.samples = length * frequency;
      if (frequency <= 0) {
        frequency = activeMicrophone.maxFrequency;
      }
      var constraints = {
        audio: {
          deviceId: activeMicrophone.device.deviceId ? { exact: activeMicrophone.device.deviceId } : undefined,
          sampleRate: frequency,
        }
      };
      navigator.mediaDevices.getUserMedia(constraints).then(stream => {
        const audioContext = new AudioContext({ sampleRate: frequency });
        const source = audioContext.createMediaStreamSource(stream);
        const processor = audioContext.createScriptProcessor(4096, 1, 1);
        processor.onaudioprocess = (event) => {
          if (!activeMicrophone || !activeMicrophone.isRecording) {
            processor.onaudioprocess = null;
            return; // microphone was stopped
          }
          if (!activeMicrophone.audioBufferPtr || !activeMicrophone.onBufferReadPtr) {
            return; // buffer is not ready
          }
          const data = event.inputBuffer.getChannelData(0); // unity only supports mono
          for (var i = 0; i < data.length; i++) {
            Module.HEAPF32[activeMicrophone.audioBufferPtr / Float32Array.BYTES_PER_ELEMENT + activeMicrophone.position] = data[i];
            activeMicrophone.position++;
            if (activeMicrophone.position >= activeMicrophone.samples) {
              if (activeMicrophone.loop) {
                activeMicrophone.position = 0;
              } else {
                activeMicrophone.position = activeMicrophone.samples - 1;
                break;
              }
            }
          }
          Module.dynCall_vi(activeMicrophone.onBufferReadPtr, activeMicrophone.position);
        };
        source.connect(processor);
        processor.connect(audioContext.destination);
        activeMicrophone.processor = processor;
        activeMicrophone.source = source;
        activeMicrophone.audioContext = audioContext;
        activeMicrophone.stream = stream;
      }).catch(error => {
        console.error(error);
        this.Microphone_StopRecording(deviceName);
      });
      activeMicrophone.isRecording = true;
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
      if (activeMicrophone !== microphone) {
        console.warn("UnityMicrophoneLibrary: recording is not active for this device");
        return 3;
      }
      if (!activeMicrophone.isRecording) {
        console.warn("UnityMicrophoneLibrary: no recording in progress");
        return 2;
      }
      try {
        activeMicrophone.processor.disconnect();
        activeMicrophone.source.disconnect();
        activeMicrophone.audioContext.close();
        activeMicrophone.stream.getTracks().forEach(track => track.stop());
      } finally {
        activeMicrophone.isRecording = false;
        activeMicrophone.audioBufferPtr = null;
        activeMicrophone.onBufferReadPtr = null;
        activeMicrophone.audioContext = null;
        activeMicrophone.processor = null;
        activeMicrophone.loop = null;
        activeMicrophone.source = null;
        activeMicrophone.stream = null;
        activeMicrophone = null;
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
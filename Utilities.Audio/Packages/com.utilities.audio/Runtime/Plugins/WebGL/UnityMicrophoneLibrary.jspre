// Licensed under the MIT License. See LICENSE in the project root for license information.

/**
 * Initializes the dynCall_* function table lookups.
 * Thanks to De-Panther for the following code.
 * Checks if specific dynCall functions exist,
 * if not, it will create them using the getWasmTableEntry function.
 * @see https://discussions.unity.com/t/makedyncall-replacing-dyncall-in-unity-6/1543088
 * @returns {void}
*/
if (typeof ENVIRONMENT_IS_PTHREAD === "undefined" || !ENVIRONMENT_IS_PTHREAD) {
  Module['preRun'].push(function () {
    initDynCalls();
  });
} else {
  // If we are in a pthread, we need to initialize the dynCalls immediately
  initDynCalls();
}
function initDynCalls() {
  if (typeof getWasmTableEntry !== "undefined") {
    Module.dynCall_v = Module.dynCall_v || function (cb) {
      return getWasmTableEntry(cb)();
    }
    Module.dynCall_vi = Module.dynCall_vi || function (cb, arg1) {
      return getWasmTableEntry(cb)(arg1);
    }
  }
}
/**
 * Queries the audio devices and populates the microphoneDevices array.
 * @param onEnumerateDevicesPtr The pointer to the onEnumerateDevices function.
 */
function queryAudioDevices(onEnumerateDevicesPtr) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
    console.error("browser not supported.");
    return;
  }
  navigator.mediaDevices.enumerateDevices().then((devices) => {
    devices.forEach((device) => {
      if (device.kind === 'audioinput') {
        let maxFrequency = 44100; // Default max frequency
        let minFrequency = 48000;  // Default min frequency
        try {
          var capabilities = device.getCapabilities();
          if (capabilities && capabilities.sampleRate) {
            maxFrequency = capabilities.sampleRate.max;
            minFrequency = capabilities.sampleRate.min;
          }
        } catch (error) {
          console.warn(`Failed to get capabilities for device: ${device.label}`);
        }
        var microphone = { name: device.label, device: device, isRecording: false, position: 0, maxFrequency, minFrequency };
        for (var i = 0; i < microphoneDevices.length; i++) {
          if (microphoneDevices[i].device.deviceId === microphone.device.deviceId) {
            return;
          }
        }
        console.log(microphone);
        microphoneDevices.push(microphone);
      }
    });
    Module.dynCall_v(onEnumerateDevicesPtr);
  }).catch((error) => {
    console.error(error);
  });
};
/**
 * Gets the microphone device by name.
 * @param deviceName The name of the microphone device.
 * @returns The microphone device.
 */
function getMicrophoneDevice(deviceName) {
  if (!deviceName) {
    for (var i = 0; i < microphoneDevices.length; i++) {
      if (microphoneDevices[i].device.deviceId === "default") {
        return microphoneDevices[i];
      }
    }
    // fallback to the first device found
    if (microphoneDevices.length > 0) {
      return microphoneDevices[0];
    }
  } else {
    for (var i = 0; i < microphoneDevices.length; i++) {
      if (microphoneDevices[i].device.label === deviceName) {
        return microphoneDevices[i];
      }
    }
  }
  throw new Error("UnityMicrophoneLibrary: device not found!");
}
/**
 * Creates a processor worklet for the microphone device.
 * @returns The MediaStream.
 */
function createWorkletProcessorURL() {
  const workletProcessorCode = `
    class MyProcessor extends AudioWorkletProcessor {
        constructor() {
            super();
            this.port.onmessage = this.onmessage.bind(this);
            this.buffer = [];
        }
        onmessage(event) {
            // Handle messages from the main script if needed
        }
        process(inputs, outputs, parameters) {
            if (inputs.length > 0){
              const inputChannel = inputs[0];
              if (inputChannel.length > 0) {
                const inputData = inputChannel[0];
                const bufferLength = inputData.length;
                // Transfer audio data to the main thread
                this.port.postMessage({ data: inputData, bufferLength: bufferLength });
              }
            }
            return true;
        }
    }
    registerProcessor('my-processor', MyProcessor);
  `;
  const blob = new Blob([workletProcessorCode], { type: 'application/javascript' });
  return URL.createObjectURL(blob);
}
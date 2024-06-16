function queryAudioDevices(onEnumerateDevicesPtr) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
    console.error("browser not supported.");
    return;
  }

  microphoneDevices = [];

  navigator.mediaDevices.enumerateDevices().then((devices) => {
    devices.forEach((device) => {
      if (device.kind === 'audioinput') {
        try {
          var capabilities = device.getCapabilities();
          var maxFrequency = capabilities.sampleRate.max;
          var minFrequency = capabilities.sampleRate.min;
          var microphone = { name: device.label, device: device, isRecording: false, position: 0, maxFrequency, minFrequency };
          for (var i = 0; i < microphoneDevices.length; i++) {
            if (microphoneDevices[i].device.deviceId === microphone.device.deviceId) {
              return;
            }
          }

          microphoneDevices.push(microphone);
        } catch (error) {
          console.warn(`Failed to get capabilities for device: ${device.label}`);
        }
      }
    });
    Module.dynCall_v(onEnumerateDevicesPtr);
  }).catch((error) => {
    console.error(error);
  });
};

function getMicrophoneDevice(deviceName) {
  if (!deviceName) {
    for (var i = 0; i < microphoneDevices.length; i++) {
      if (microphoneDevices[i].device.deviceId === "default") {
        return microphoneDevices[i];
      }
    }
  } else {
    for (var i = 0; i < microphoneDevices.length; i++) {
      if (microphoneDevices[i].name === deviceName) {
        return microphoneDevices[i];
      }
    }
  }

  throw new Error("UnityMicrophoneLibrary: device not found!");
}

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
            const input = inputs[0];
            if (input.length > 0) {
                const inputData = input[0];
                const bufferLength = inputData.length;

                // Transfer data to the main thread
                this.port.postMessage({ data: inputData, bufferLength: bufferLength });
            }

            return true;
        }
    }

    registerProcessor('my-processor', MyProcessor);
  `;

  const blob = new Blob([workletProcessorCode], { type: 'application/javascript' });
  return URL.createObjectURL(blob);
}
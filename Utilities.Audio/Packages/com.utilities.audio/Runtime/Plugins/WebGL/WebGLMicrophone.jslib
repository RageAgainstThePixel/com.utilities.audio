var WebGLMicrophone = {
  InitMicrophoneJS: function(callbackPtr, bufferPtr, bufferSize) {
    if (!navigator.mediaDevices ||
        !navigator.mediaDevices.enumerateDevices ||
        !navigator.mediaDevices.getUserMedia) {
      alert('WebGLMicrophone is not supported in this browser!');
      return;
    }

    const constraints = {
      audio: {
        optional: [{ sourceId: "audioSource" }]
      }
    };

    navigator.mediaDevices.getUserMedia(constraints).then(_ => {
      console.log("WebGLMicrophone permissions granted!");
      document.microphoneContext = new Object();
      document.microphoneContext.audioContext = new AudioContext();
      document.microphoneContext.devices = [];
      document.microphoneContext.pcmData = [];
      document.microphoneContext.bufferPtr = bufferPtr;
      document.microphoneContext.buffer = new Float32Array(bufferSize);
      document.microphoneContext.callbackPtr = callbackPtr;
      document.microphoneContext.isRecording = false;
      WebGLMicrophone.queryAudioInput();
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });

    setInterval(function() {
      if (document.microphoneContext) {
        WebGLMicrophone.queryAudioInput();

        const audioContext = document.microphoneContext.audioContext;

        if (!audioContext) { return; }
        if (audioContext.state === "suspended" || audioContext.state === "interrupted") {
          console.log(`audioContext.state::changed -> ${audioContext.state}`);
          audioContext.resume();
        }
      }
    }, 300);
  },

  queryAudioInput: function() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
      console.error("enumerateDevices() not supported.");
      return;
    }

    navigator.mediaDevices.enumerateDevices().then(function(devices) {
      if (!document.microphoneContext) { return; }
      document.microphoneContext.devices = [];
      devices.forEach(function(device) {
        console.log(`${device.kind}: ${device.label} id=${device.deviceId}`);

        if (device.kind === 'audioinput') {
          document.microphoneContext.devices.push(device.label);
        }
      });
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });
  },

  GetNumberOfMicrophonesJS: function() {
    if (!document.microphoneContext || !document.microphoneContext.devices) {
      return 0;
    }

    return document.microphoneContext.devices.length;
  },

  GetMicrophoneDeviceNameJS: function(index) {
    if (!document.microphoneContext || !document.microphoneContext.devices) {
      return null;
    }

    const devices = document.microphoneContext.devices;

    if (index >= 0 && index < devices.length) {
      var deviceName = devices[index];
      var bufferSize = lengthBytesUTF8(deviceName) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(deviceName, buffer, bufferSize);
      return buffer;
    }

    return null;
  },

  StartRecordingJS: function(sampleRate) {
    if (!document.microphoneContext || !document.microphoneContext.audioContext) {
      console.error("WebGLMicrophone: not initialized!");
      return;
    }

    if (!navigator.mediaDevices.getUserMedia) {
      console.error('WebGLMicrophone: not supported in this browser!');
      return;
    }

    const micContext = document.microphoneContext;

    if (micContext.isRecording) {
      console.warn("WebGLMicrophone: recording already in progress")
      return;
    }

    navigator.mediaDevices.getUserMedia({ audio: true }).then(function(stream) {
      micContext.analyser = micContext.audioContext.createAnalyser();
      micContext.analyser.minDecibels = -90;
      micContext.analyser.maxDecibels = -10;
      micContext.analyser.smoothingTimeConstant = 0.85;

      var options = {
        mimeType: "audio/webm",
        audioBitsPerSecond: sampleRate
      };

      micContext.mediaRecorder = new MediaRecorder(stream, options);
      micContext.source = micContext.audioContext.createMediaStreamSource(stream);
      micContext.source.connect(micContext.analyser);
      micContext.mediaRecorder.addEventListener("dataavailable", WebGLMicrophone.microphoneDataHandler);
      micContext.mediaRecorder.start();
      micContext.recorderIntervalId = setInterval(function () {
        document.mediaRecorder.stop();
        document.mediaRecorder.start();
      }, 1000 / 30);

      micContext.isRecording = true;
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });
  },

  microphoneDataHandler: async function(event) {
    const micContext = document.microphoneContext;
    micContext.analyser.getFloatTimeDomainData(micContext.buffer);

    for (var i = 0; i < micContext.buffer.length; ++i) {
      micContext.pcmData.push(micContext.buffer[i]);
    }

    if (micContext.pcmData.length > micContext.mediaRecorder.audioBitsPerSecond) {
      micContext.pcmData = micContext.pcmData.slice(micContext.pcmData.length - micContext.mediaRecorder.audioBitsPerSecond);
    }

    const size = micContext.pcmData.length;

    for (var i = 0; i < size; ++i) {
      setValue(micContext.bufferPtr + 4 * i, micContext.pcmData[i], 'float');
    }

    // Invoke the callback with the number of samples
    Runtime.dynCall('vi', micContext.callbackPtr, [size]);
  },

  GetCurrentMicrophonePositionJS: function() {
    return document.microphoneContext ? document.microphoneContext.pcmData.length : 0;
  },

  StopRecordingJS: function() {
    if (!document.microphoneContext) {
      console.error("WebGLMicrophone: not initialized!");
      return;
    }

    const micContext = document.microphoneContext;

    if (!micContext.isRecording) {
      console.warn("WebGLMicrophone: no recording in progress")
      return;
    }

    clearInterval(micContext.recorderIntervalId);
    micContext.mediaRecorder.stop();
    micContext.mediaRecorder.removeEventListener("dataavailable", WebGLMicrophone.microphoneDataHandler);
    micContext.source.disconnect(micContext.analyser);
    micContext.source = null;
    micContext.analyser = null;
    micContext.mediaRecorder = null;
    micContext.isRecording = false;
  },

  IsRecordingJS: function() {
    if (!document.microphoneContext) {
      return false;
    }

    return document.microphoneContext.isRecording === true;
  }
}

mergeInto(LibraryManager.library, WebGLMicrophone);

var Microphone = {
  InitMicrophoneJS: function() {
    if (navigator.mediaDevices === undefined || navigator.mediaDevices.enumerateDevices === undefined) {
      alert('WebGLMicrophone is not supported in this browser!');
      return;
    }

    const userMediaConstraints = {
      video: false,
      audio: {
        channelCount: 2,
      }
    };
    navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia || navigator.mozGetUserMedia || navigator.msGetUserMedia;
    navigator.mediaDevices.getUserMedia(userMediaConstraints).then(_ => {
      console.log("WebGLMicrophone permissions granted!");
      document.microphoneContext = new Object();
      document.microphoneContext.devices = [];
      document.microphoneContext.position = 0;
      document.microphoneContext.isRecording = false;
      document.microphoneContext.audioContext = new AudioContext({"sampleRate": 44100});

      navigator.mediaDevices.enumerateDevices().then(function(devices) {
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
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });

    setInterval(function() {
      if (document.microphoneContext === undefined) {
        console.log("Waiting for WebGLMicrophone permissions")
        return;
      }

      var audioContext = document.microphoneContext.audioContext;

      if (audioContext.state === "suspended" || audioContext.state === "interrupted") {
        console.log(`audioContext.state::changed -> ${audioContext.state}`);
        audioContext.resume();
      }
    }, 300);
  },

  GetNumberOfMicrophonesJS: function() {
    if (document.microphoneContext == undefined || document.microphoneContext.devices === undefined) {
      return 0;
    }

    return document.microphoneContext.devices.length;
  },

  GetMicrophoneDeviceNameJS: function(index) {
    if (document.microphoneContext == undefined || document.microphoneContext.devices === undefined) {
      return null;
    }

    var devices = document.microphoneContext.devices;

    if (index >= 0 && index < devices.length) {
      var deviceName = devices[index];
      var bufferSize = lengthBytesUTF8(deviceName) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(deviceName, buffer, bufferSize);
      return buffer;
    }

    return null;
  },

  StartRecordingJS: function(functionPtr, bufferPtr) {
    if (navigator.mediaDevices.getUserMedia) {
      navigator.mediaDevices.getUserMedia({ audio: true }).then(function(stream) {
        var audioContext = document.microphoneContext.audioContext;
        var source = audioContext.createMediaStreamSource(stream);
        const numberOfInputChannels = 1; // Mono audio
        const numberOfOutputChannels = 1;
        const bufferSize = 4096;

        if (audioContext.createScriptProcessor) {
          document.microphoneContext.recorder = audioContext.createScriptProcessor(bufferSize, numberOfInputChannels, numberOfOutputChannels);
        } else {
          document.microphoneContext.recorder = audioContext.createJavaScriptNode(bufferSize, numberOfInputChannels, numberOfOutputChannels);
        }

        document.microphoneContext.recorder.onaudioprocess = function (stream) {
          var pcmData = stream.inputBuffer.getChannelData(0);
          var floatBuffer = new Float32Array(pcmData.buffer);
          Module.HEAPF32.set(floatBuffer, bufferPtr >> 2);
          Module.dynCall_vii(functionPtr, pcmData.length, bufferPtr);
        };

        source.connect(document.microphoneContext.recorder);
        document.microphoneContext.recorder.connect(audioContext.destination);
        document.microphoneContext.source = source;
        document.microphoneContext.isRecording = true;

        console.log("WebGLMicrophone started recording");
      }).catch(function(error) {
          console.log(`Failed in GetUserMedia: ${error}`);
      });
    }
  },

  StopRecordingJS: function() {
    if (document.microphoneContext == undefined) {
      console.error("WebGLMicrophone not initialized!");
      return;
    }

    document.microphoneContext.source.disconnect(document.microphoneContext.recorder);
    document.microphoneContext.source = null;
    document.microphoneContext.recorder.disconnect();
    document.microphoneContext.recorder = null;
    document.microphoneContext.isRecording = false;
  },

  IsRecordingJS: function() {
    if (document.microphoneContext == undefined) {
      return false;
    }

    return document.microphoneContext.isRecording === true;
  }
}

mergeInto(LibraryManager.library, Microphone);

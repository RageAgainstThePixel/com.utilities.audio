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
      queryAudioInput();
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });

    setInterval(function() {
      if (document.microphoneContext) {
        queryAudioInput();

        const audioContext = document.microphoneContext.audioContext;

        if (!audioContext) { return; }
        if (audioContext.state === "suspended" || audioContext.state === "interrupted") {
          console.log(`audioContext.state::changed -> ${audioContext.state}`);
          audioContext.resume();
        }
      }
    }, 300);
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
    console.log("StartRecordingJS");
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

    micContext.isRecording = true;
    navigator.mediaDevices.getUserMedia({ audio: true }).then(function(stream) {
      micContext.analyser = micContext.audioContext.createAnalyser();
      micContext.analyser.minDecibels = -90;
      micContext.analyser.maxDecibels = -10;
      micContext.analyser.smoothingTimeConstant = 0.85;
      micContext.source = micContext.audioContext.createMediaStreamSource(stream);
      micContext.source.connect(micContext.analyser);
      micContext.mediaRecorder = new MediaRecorder(stream, {
        mimeType: "audio/webm",
        audioBitsPerSecond: sampleRate
      });
      micContext.mediaRecorder.addEventListener("dataavailable", microphoneDataHandler);
      micContext.micDataHandler = microphoneDataHandler;
      micContext.mediaRecorder.start();
      micContext.recorderIntervalId = setInterval(function () {
        micContext.mediaRecorder.stop();
        micContext.mediaRecorder.start();
      }, 1000 / 30);
    }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
    });
  },


  GetCurrentMicrophonePositionJS: function() {
    return document.microphoneContext ? document.microphoneContext.pcmData.length : 0;
  },

  StopRecordingJS: function() {
    console.log("StopRecordingJS");
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
    micContext.mediaRecorder = null;
    micContext.source.disconnect(micContext.analyser);
    micContext.source = null;
    micContext.analyser = null;
    micContext.isRecording = false;
  },

  IsRecordingJS: function() {
    if (!document.microphoneContext) {
      return false;
    }

    return document.microphoneContext.isRecording == true;
  }
}

mergeInto(LibraryManager.library, WebGLMicrophone);

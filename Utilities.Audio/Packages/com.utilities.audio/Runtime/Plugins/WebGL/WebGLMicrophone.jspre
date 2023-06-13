function queryAudioInput() {
  if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
    console.error("enumerateDevices() not supported.");
    return;
  }

  navigator.mediaDevices.enumerateDevices().then(function(devices) {
    if (!document.microphoneContext) { return; }
    document.microphoneContext.devices = [];
    devices.forEach(function(device) {
    // console.log(`${device.kind}: ${device.label} id=${device.deviceId}`);
    if (device.kind === 'audioinput') {
      document.microphoneContext.devices.push(device.label);
    }
    });
  }).catch(function(error) {
      console.error(`${error.name}: ${error.message}`);
  });
};

async function microphoneDataHandler(event) {
  try {
    console.log("microphoneDataHandler::start");
    const micContext = document.microphoneContext;
    micContext.analyser.getFloatTimeDomainData(micContext.buffer);
    for (var i = 0; i < micContext.buffer.length; ++i) {
      micContext.pcmData.push(micContext.buffer[i]);
    }
    if (micContext.pcmData.length > micContext.mediaRecorder.audioBitsPerSecond) {
      micContext.pcmData = micContext.pcmData.slice(micContext.pcmData.length - micContext.mediaRecorder.audioBitsPerSecond);
    }
    console.log("micContext:", micContext);
    const size = micContext.pcmData.length;
    //for (var i = 0; i < size; ++i) {
    //  setValue(micContext.bufferPtr + 4 * i, micContext.pcmData[i], 'float');
    //}
    console.log("invoke callback");
    // Invoke the callback with the number of samples
    //Module.dynCall_vi(micContext.callbackPtr, size);
    Module.dynCall_v(micContext.callbackPtr);
    console.log("microphoneDataHandler::end");
  } catch (error) {
    console.error(error);
  }
};
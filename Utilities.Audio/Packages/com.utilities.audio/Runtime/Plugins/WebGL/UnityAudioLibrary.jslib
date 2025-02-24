// Licensed under the MIT License. See LICENSE in the project root for license information.

var UnityAudioLibrary = {
  /**
   * Pointer index for audio playback objects.
   */
  $ptrIndex: 0,
  /**
   * Array of instanced audio playback objects.
   */
  $audioPtrs: [],
  /**
   * Initializes the audio playback context.
   * This is used in place of missing OnAudioFilterRead in Unity.
   * The processAudio function is called every 20ms to match the OnAudioFilterRead callback frequency in Unity.
   * @param {number} playbackSampleRate The sample rate of the audio playback context.
   * @returns A pointer to the audio playback context.
   */
  AudioStream_InitPlayback: function (playbackSampleRate) {
    try {
      const audioPtr = ++ptrIndex;
      const AudioContext = window.AudioContext || window.webkitAudioContext;
      const audioContext = new AudioContext({ sampleRate: playbackSampleRate });
      const gain = audioContext.createGain();
      gain.connect(audioContext.destination);
      function processAudio() {
        function setPlaybackInterval(instance) {
          if (instance.playbackInterval === 0) {
            // console.log(`[${audioPtr}] setting new processAudio interval.`);
            instance.playbackInterval = setInterval(processAudio, 20);
          }
          // console.log(`[${audioPtr}] processAudio, sleeping for 20ms.`);
        }
        try {
          // console.log(`Processing audio for pointer ${audioPtr}.`);
          const instance = audioPtrs[audioPtr];
          if (instance == null) {
            throw new Error(`Audio context with pointer ${audioPtr} not found.`);
          }
          if (instance.audioContext.state === 'suspended') {
            // console.log(`AudioContext state is suspended, attempting to resume.`);
            instance.audioContext.resume();
            setPlaybackInterval(instance);
            return;
          } else if (instance.chunkQueue.length === 0) {
            // console.log(`No chunks to process.`);
            setPlaybackInterval(instance);
            return;
          }
          clearTimeout(instance.playbackInterval);
          instance.playbackInterval = 0;
          const chunkCount = Math.min(5, instance.chunkQueue.length);
          const maxDuration = chunkCount * playbackSampleRate;
          // console.log(`[${audioPtr}] Processing ${chunkCount} chunks of ${instance.chunkQueue.length} for a max duration ${maxDuration / playbackSampleRate}`);
          const audioBuffer = instance.audioContext.createBuffer(1, maxDuration, playbackSampleRate);
          let bufferPosition = 0;
          for (let i = 0; i < chunkCount; i++) {
            const nextChunk = instance.chunkQueue.shift();
            audioBuffer.copyToChannel(nextChunk, 0, bufferPosition);
            bufferPosition += nextChunk.length;
          }
          const duration = bufferPosition / playbackSampleRate;
          if (instance.activeSource != null) {
            instance.activeSource.stop();
            instance.activeSource.disconnect();
            instance.activeSource = null;
          }
          const activeSource = instance.audioContext.createBufferSource();
          activeSource.buffer = audioBuffer;
          activeSource.connect(gain);
          activeSource.onended = processAudio;
          activeSource.start(0, 0, duration);
          // console.log(`[${audioPtr}] Playing audio for ${duration} seconds.`);
          instance.activeSource = activeSource;
        } catch (error) {
          console.error(error);
        }
      }
      // console.log(`Set playback interval for pointer ${audioPtr}.`);
      const playbackInterval = setInterval(processAudio, 20);
      audioPtrs[audioPtr] = {
        chunkQueue: [], // as Float32Array[]
        audioContext: audioContext,
        gain: gain,
        activeSource: null,
        playbackInterval: playbackInterval,
      };
      // console.log(`Initialized audio context with pointer ${audioPtr}.`);
      return audioPtr;
    } catch (error) {
      console.error(error);
      return 0;
    }
  },
  /**
   * Appends the audio buffer to the audio playback context.
   * @param {number} audioPtr The pointer to the audio playback context.
   * @param {number} bufferPtr The pointer to the audio buffer. This buffer is an array of pcm-s16 floats.
   * @param {number} bufferLength The length of the audio buffer.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred.
   */
  AudioStream_AppendBufferPlayback: function (audioPtr, bufferPtr, bufferLength) {
    try {
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      if (instance.audioContext.state === 'suspended') {
        // don't process any audio if the context is suspended
        return 0;
      }
      const chunk = new Float32Array(Module.HEAPF32.buffer, bufferPtr, bufferLength);
      instance.chunkQueue.push(chunk);
      // console.log(`[${audioPtr}] Appended buffer of length ${bufferLength}.`);
      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  /**
   * Sets the volume of the audio playback context.
   * @param {number} audioPtr The pointer to the audio playback context.
   * @param {number} volume The volume to set.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred.
   */
  AudioStream_SetVolume: function (audioPtr, volume) {
    try {
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      if (instance.audioContext.state === 'suspended') {
        // don't process any audio if the context is suspended
        return 0;
      }
      instance.gain.gain.value = volume;
      // console.log(`[${audioPtr}] Set volume to ${volume}`);
      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  /**
   * Disposes the audio playback context.
   * @param {number} audioPtr The pointer to the audio playback context.
   */
  AudioStream_Dispose: function (audioPtr) {
    try {
      if (audioPtr === 0) { return; }
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      try {
        // console.log(`Disposing audio context with pointer ${audioPtr}.`);
        clearInterval(instance.playbackInterval);
        instance.playbackInterval = 0;
        if (instance.activeSource != null) {
          instance.activeSource.stop();
          instance.activeSource.disconnect();
          instance.activeSource = null;
        }
        instance.gain.disconnect();
        instance.gain = null;
        instance.audioContext.close();
        instance.audioContext = null;
      } finally {
        delete audioPtrs[audioPtr];
      }
    } catch (error) {
      console.error(error);
    }
  }
}
autoAddDeps(UnityAudioLibrary, '$ptrIndex');
autoAddDeps(UnityAudioLibrary, '$audioPtrs');
mergeInto(LibraryManager.library, UnityAudioLibrary);
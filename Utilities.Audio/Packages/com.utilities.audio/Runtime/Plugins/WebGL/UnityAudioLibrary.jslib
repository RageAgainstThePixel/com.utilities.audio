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
   * @returns A pointer to the audio playback context.
   */
  InitAudioStreamPlayback: function () {
    try {
      if (!("MediaSource" in window)) {
        throw new Error('MediaSource is not supported in this browser!');
      }
      const mediaSource = new MediaSource();
      const audio = new Audio();
      const audioPtr = ++ptrIndex;
      audioPtrs[audioPtr] = {
        appendQueue: [],
        audio: audio,
        mediaSource: mediaSource,
      };
      audio.src = URL.createObjectURL(mediaSource);
      audio.play();
      mediaSource.addEventListener('sourceopen', function () {
        try {
          const sourceBuffer = mediaSource.addSourceBuffer('audio/wav');
          sourceBuffer.mode = 'sequence';
          audioPtrs[audioPtr].updateInterval = setInterval(() => {
            try {
              const instance = audioPtrs[audioPtr];
              if (instance == null) {
                throw new Error(`audio context pointer ${audioPtr} not found!`);
              }
              if (instance.appendQueue.length > 0 && !sourceBuffer.updating) {
                sourceBuffer.appendBuffer(instance.appendQueue.shift());
              }
            } catch (error) {
              console.error(error);
            }
          }, 50);
        } catch (error) {
          console.error(error);
        }
      });
      return audioPtr;
    } catch (error) {
      console.error(error);
      return 0;
    }
  },
  /**
   * Appends the audio buffer to the audio playback context.
   * @param {number} audioPtr The pointer to the audio playback context.
   * @param {number} bufferPtr The pointer to the audio buffer.
   * @param {number} bufferLength The length of the audio buffer.
   * @returns {number} The status code. 0 if successful, 1 if an error occurred.
   */
  AppendBufferPlayback: function (audioPtr, bufferPtr, bufferLength) {
    try {
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      const buffer = new Uint8Array(Module.HEAPU8.buffer, bufferPtr, bufferLength);
      instance.appendQueue.push(buffer);
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
  SetVolume: function (audioPtr, volume) {
    try {
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      instance.audio.volume = volume;
      return 0;
    } catch (error) {
      console.error(error);
      return 1;
    }
  },
  Dispose: function (audioPtr) {
    try {
      if (audioPtr === 0) { return; }
      const instance = audioPtrs[audioPtr];
      if (instance == null) {
        throw new Error(`Audio context with pointer ${audioPtr} not found.`);
      }
      try {
        clearInterval(instance.updateInterval);
        instance.audio.pause();
        URL.revokeObjectURL(instance.audio.src);
        instance.audio = null;
        instance.mediaSource.endOfStream();
        instance.mediaSource = null;
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
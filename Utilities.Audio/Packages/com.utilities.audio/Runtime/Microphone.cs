// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

#if UNITY_WEBGL //&& !UNITY_EDITOR
using AOT;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
#endif

namespace Utilities.Audio
{
    /// <summary>
    /// A wrapper class for <see cref="UnityEngine.Microphone"/>
    /// </summary>
    public class Microphone
    {
#if UNITY_WEBGL //&& !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init()
        {
            nativeBuffer = new float[1024];
            var pcmDataBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(nativeBuffer, 0);
            InitMicrophoneJS(PCMReaderCallback, pcmDataBuffer, nativeBuffer.Length);
        }

        #region Interop

        [DllImport("__Internal")]
        private static extern void InitMicrophoneJS(Action pcmReaderCallback, IntPtr buffer, int bufferSize);

        [DllImport("__Internal")]
        private static extern int GetNumberOfMicrophonesJS();

        [DllImport("__Internal")]
        private static extern string GetMicrophoneDeviceNameJS(int index);

        [DllImport("__Internal")]
        private static extern void StartRecordingJS(int channels);

        [DllImport("__Internal")]
        private static extern int GetCurrentMicrophonePositionJS();

        [DllImport("__Internal")]
        private static extern void StopRecordingJS();

        [DllImport("__Internal")]
        private static extern bool IsRecordingJS();

        #endregion Interop

        private static bool loop;

        private static float[] audioBuffer;
        private static float[] nativeBuffer;

        private static AudioClip currentClip;

        private static readonly HashSet<string> deviceList = new HashSet<string>();
#endif
        private static bool isRecording;

        /// <summary>
        /// A list of available microphone devices, identified by name.
        /// </summary>
#pragma warning disable IDE1006
        // ReSharper disable once InconsistentNaming
        public static string[] devices
#pragma warning restore IDE1006
        {
#if UNITY_WEBGL //&& !UNITY_EDITOR
            get
            {
                var size = GetNumberOfMicrophonesJS();

                for (var index = 0; index < size; ++index)
                {
                    var deviceName = GetMicrophoneDeviceNameJS(index);

                    if (string.IsNullOrWhiteSpace(deviceName)) { continue; }

                    deviceList.Add(deviceName);
                }

                return deviceList.ToArray();
            }
#else
            get => UnityEngine.Microphone.devices;
#endif
        }

        /// <summary>
        /// Start Recording with device.
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="loop">Indicates whether the recording should continue recording if lengthSec is reached, and wrap around and record from the beginning of the AudioClip.</param>
        /// <param name="lengthSec">Is the length of the AudioClip produced by the recording.</param>
        /// <param name="frequency">The sample rate of the AudioClip produced by the recording.</param>
        /// <returns>
        /// The function returns null if the recording fails to start.
        /// </returns>
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            switch (lengthSec)
            {
                case <= 0: throw new ArgumentException($"Length of recording must be greater than zero seconds (was: {lengthSec} seconds)");
                case > 3600: throw new ArgumentException($"Length of recording must be less than one hour (was: {lengthSec} seconds)");
            }

            if (frequency <= 0) { throw new ArgumentException($"Frequency of recording must be greater than zero (was: {frequency} Hz)"); }

            if (isRecording)
            {
                Debug.LogError("A recording session is already in progress!");
                return null;
            }

            isRecording = true;
#if UNITY_WEBGL //&& !UNITY_EDITOR
            Microphone.loop = loop;
            currentClip = AudioClip.Create("WebMic_Recording", frequency * lengthSec, 1, frequency, false);
            audioBuffer = new float[currentClip.samples];
            currentClip.SetData(audioBuffer, 0);
            StartRecordingJS(frequency);
            return currentClip;
#else
            return UnityEngine.Microphone.Start(deviceName, loop, lengthSec, frequency);
#endif
        }

        /// <summary>
        /// Stops recording.
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        public static void End(string deviceName)
        {
            if (!isRecording) { return; }
            isRecording = false;
#if UNITY_WEBGL //&& !UNITY_EDITOR
            StopRecordingJS();
            currentClip = null;
#else
            UnityEngine.Microphone.End(deviceName);
#endif
        }

        /// <summary>
        /// Query if a device is currently recording.
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        /// <returns></returns>
        public static bool IsRecording(string deviceName)
        {
#if UNITY_WEBGL //&& !UNITY_EDITOR
            return IsRecordingJS();
#else
            return UnityEngine.Microphone.IsRecording(deviceName);
#endif
        }

        /// <summary>
        /// Get the frequency capabilities of a device.
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="minFreq">Returns the minimum sampling frequency of the device.</param>
        /// <param name="maxFreq">Returns the maximum sampling frequency of the device.</param>
        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            // When a value of zero is returned in the minFreq and maxFreq parameters,
            // this indicates that the device supports any frequency.
#if UNITY_WEBGL //&& !UNITY_EDITOR
            minFreq = 8000;
            maxFreq = 96000;
#else
            UnityEngine.Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
#endif
        }

        /// <summary>
        /// Get the position in samples of the recording.
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        public static int GetPosition(string deviceName)
        {
            if (!isRecording) { return 0; }
#if UNITY_WEBGL //&& !UNITY_EDITOR
            return GetCurrentMicrophonePositionJS();
#else
            return UnityEngine.Microphone.GetPosition(deviceName);
#endif
        }

#if UNITY_WEBGL //&& !UNITY_EDITOR

        [MonoPInvokeCallback(typeof(Action))]
        private static void PCMReaderCallback()
        {
            Debug.Log(nameof(PCMReaderCallback));
            //if (currentClip == null) { return; }

            //var currentPosition = GetCurrentMicrophonePositionJS();

            //for (int i = 0; i < size; ++i)
            //{
            //    audioBuffer[currentPosition] = nativeBuffer[i];
            //    currentPosition++;

            //    if (currentPosition >= audioBuffer.Length)
            //    {
            //        if (loop)
            //        {
            //            currentPosition = 0;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}

            //currentClip.SetData(audioBuffer, 0);
        }
#endif
    }
}

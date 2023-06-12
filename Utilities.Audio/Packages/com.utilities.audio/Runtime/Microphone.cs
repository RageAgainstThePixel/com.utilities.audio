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
            InitMicrophoneJS();
        }

        #region Interop

        [DllImport("__Internal")]
        private static extern void InitMicrophoneJS();

        [DllImport("__Internal")]
        private static extern int GetNumberOfMicrophonesJS();

        [DllImport("__Internal")]
        private static extern string GetMicrophoneDeviceNameJS(int index);

        [DllImport("__Internal")]
        private static extern void StartRecordingJS(Action<int, IntPtr> callback, float[] buffer, int bufferSize);

        [DllImport("__Internal")]
        private static extern void StopRecordingJS();

        [DllImport("__Internal")]
        private static extern bool IsRecordingJS();

        #endregion Interop

        private static bool loop;
        private static int currentPosition;
        private static int frequency = 41000;
        private static float[] audioBuffer;
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
            Microphone.frequency = frequency;
            var channels = 1;
            currentPosition = 0;
            audioBuffer = new float[frequency * lengthSec /* * channels*/];
            StartRecordingJS(StreamCallback, audioBuffer, audioBuffer.Length);
            currentClip = AudioClip.Create("WebMic_Recording", frequency * lengthSec, channels, frequency, false);
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
            minFreq = frequency;
            maxFreq = frequency;
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
            return currentPosition;
#else
            return UnityEngine.Microphone.GetPosition(deviceName);
#endif
        }

#if UNITY_WEBGL //&& !UNITY_EDITOR

        [MonoPInvokeCallback(typeof(Action<int, IntPtr>))]
        private static void StreamCallback(int size, IntPtr dataPtr)
        {
            var samplingData = new float[size];
            Marshal.Copy(dataPtr, samplingData, 0, size);

            foreach (var sample in samplingData)
            {
                audioBuffer[currentPosition] = sample;
                currentPosition++;

                if (currentPosition == audioBuffer.Length)
                {
                    if (loop)
                    {
                        currentPosition = 0;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (currentClip != null)
            {
                currentClip.SetData(audioBuffer, 0);
            }
        }
#endif
    }
}

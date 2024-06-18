// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
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
    public static class Microphone
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Init() => Microphone_Init(Microphone_OnEnumerateDevices);

        #region Interop

        [DllImport("__Internal")]
        private static extern void Microphone_Init(Microphone_OnEnumerateDevicesDelegate onEnumerateDevices);

        private delegate void Microphone_OnEnumerateDevicesDelegate();

        [MonoPInvokeCallback(typeof(Microphone_OnEnumerateDevicesDelegate))]
        private static void Microphone_OnEnumerateDevices()
        {
            var tempDeviceList = new HashSet<string>();
            var size = Microphone_GetNumberOfDevices();

            for (var index = 0; index < size; ++index)
            {
                var deviceName = Microphone_GetDeviceName(index);

                if (string.IsNullOrWhiteSpace(deviceName)) { continue; }

                tempDeviceList.Add(deviceName);
            }

            deviceList = tempDeviceList.ToArray();
        }

        [DllImport("__Internal")]
        private static extern int Microphone_GetNumberOfDevices();

        [DllImport("__Internal")]
        private static extern string Microphone_GetDeviceName(int index);

        [DllImport("__Internal")]
        private static extern int Microphone_StartRecording(string deviceName, bool loop, int length, int frequency, Microphone_OnBufferReadDelegate onBufferRead, float[] audioBufferPtr);

        private delegate void Microphone_OnBufferReadDelegate(int position);

        [MonoPInvokeCallback(typeof(Microphone_OnBufferReadDelegate))]
        private static void Microphone_OnBufferRead(int position)
        {
            if (currentClip != null)
            {
                currentClip.SetData(audioBuffer, 0);
                currentPosition = position;
            }
        }

        [DllImport("__Internal")]
        private static extern int Microphone_StopRecording(string deviceName);

        [DllImport("__Internal")]
        private static extern bool Microphone_IsRecording(string deviceName);

        [DllImport("__Internal")]
        private static extern int Microphone_GetPosition(string deviceName);

        #endregion Interop

        private static AudioClip currentClip;
        private static int currentPosition = -1;
        private static float[] audioBuffer = Array.Empty<float>();
        private static string[] deviceList = Array.Empty<string>();
#endif
        private static bool isRecording;

        /// <summary>
        ///   <para>A list of available microphone devices, identified by name.</para>
        /// </summary>
#pragma warning disable IDE1006
        // ReSharper disable once InconsistentNaming
        public static string[] devices
#pragma warning restore IDE1006
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            get => deviceList;
#else
            get => UnityEngine.Microphone.devices;
#endif
        }

        /// <summary>
        ///   <para>Start Recording with device.</para>
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="loop">Indicates whether the recording should continue recording if length is reached, and wrap around and record from the beginning of the AudioClip.</param>
        /// <param name="length">Is the length of the AudioClip produced by the recording in seconds.</param>
        /// <param name="frequency">The sample rate of the AudioClip produced by the recording.</param>
        /// <returns>
        ///   <para>The function returns null if the recording fails to start.</para>
        /// </returns>
        public static AudioClip Start(string deviceName, bool loop, int length, int frequency)
        {
            switch (length)
            {
                case <= 0: throw new ArgumentException($"Length of recording must be greater than zero seconds (was: {length} seconds)");
                case > 3600: throw new ArgumentException($"Length of recording must be less than one hour (was: {length} seconds)");
            }

            if (frequency <= 0) { throw new ArgumentException($"Frequency of recording must be greater than zero (was: {frequency} Hz)"); }

            if (isRecording)
            {
                Debug.LogError("A recording session is already in progress!");
                return null;
            }

            isRecording = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            currentPosition = -1;
            var samples = frequency * length;
            audioBuffer = new float[samples];
            currentClip = AudioClip.Create("WebMic_Recording", samples, 1, frequency, false);

            if (audioBuffer.Length != currentClip.samples)
            {
                Debug.LogError($"Failed to create audioBuffer with proper size! {audioBuffer.Length} != {currentClip.samples}");
                UnityEngine.Object.Destroy(currentClip);
                return null;
            }

            var result = Microphone_StartRecording(deviceName, loop, length, frequency, Microphone_OnBufferRead, audioBuffer);

            if (result > 0)
            {
                Debug.LogError("Failed to start recording!");
                isRecording = false;
                return null;
            }

            return currentClip;
#else
            return UnityEngine.Microphone.Start(deviceName, loop, length, frequency);
#endif
        }

        /// <summary>
        ///   <para>Stops recording.</para>
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        public static void End(string deviceName)
        {
            if (!isRecording) { return; }
            isRecording = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            var result = Microphone_StopRecording(deviceName);

            if (result > 0)
            {
                Debug.LogError("An error occurred when attempting to stop recording!");
            }

            currentClip = null;
#else
            UnityEngine.Microphone.End(deviceName);
#endif
        }

        /// <summary>
        ///   <para>Query if a device is currently recording.</para>
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        public static bool IsRecording(string deviceName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Microphone_IsRecording(deviceName);
#else
            return UnityEngine.Microphone.IsRecording(deviceName);
#endif
        }

        /// <summary>
        ///   <para>Get the frequency capabilities of a device.</para>
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="minFreq">Returns the minimum sampling frequency of the device.</param>
        /// <param name="maxFreq">Returns the maximum sampling frequency of the device.</param>
        /// <remarks>
        ///  <para>When a value of zero is returned in the minFreq and maxFreq parameters, this indicates that the device supports any frequency.</para>
        /// </remarks>
        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL only supports 44100 Hz
            minFreq = 44100;
            maxFreq = 44100;
#else
            UnityEngine.Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
#endif
        }

        /// <summary>
        ///   <para>Get the position in samples of the recording.</para>
        /// </summary>
        /// <param name="deviceName">The name of the device.</param>
        public static int GetPosition(string deviceName)
        {
            if (!isRecording) { return 0; }
#if UNITY_WEBGL && !UNITY_EDITOR
            return currentPosition;
#else
            return UnityEngine.Microphone.GetPosition(deviceName);
#endif
        }
    }
}

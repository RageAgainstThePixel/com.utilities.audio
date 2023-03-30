// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

#if UNITY_WEBGL
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
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern int GetNumberOfMicrophones();

        [DllImport("__Internal")]
        private static extern string GetMicrophoneDeviceName(int index);

        private static readonly HashSet<string> deviceList = new HashSet<string>();
#endif

        // ReSharper disable once InconsistentNaming
        public static string[] devices
        {
#if UNITY_WEBGL
            get
            {
                var size = GetNumberOfMicrophones();

                for (var index = 0; index < size; ++index)
                {
                    var deviceName = GetMicrophoneDeviceName(index);
                    deviceList.Add(deviceName);
                }

                return deviceList.ToArray();
            }
#else
            get => UnityEngine.Microphone.devices;
#endif
        }

        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
        {
            switch (lengthSec)
            {
                case <= 0:
                    throw new ArgumentException($"Length of recording must be greater than zero seconds (was: {lengthSec} seconds)");
                case > 3600:
                    throw new ArgumentException($"Length of recording must be less than one hour (was: {lengthSec} seconds)");
            }

            if (frequency <= 0)
            {
                throw new ArgumentException($"Frequency of recording must be greater than zero (was: {frequency} Hz)");
            }

#if UNITY_WEBGL
            throw new NotImplementedException();
#else
            return UnityEngine.Microphone.Start(deviceName, loop, lengthSec, frequency);
#endif
        }

        public static void End(string deviceName)
        {
#if UNITY_WEBGL
            throw new NotImplementedException();
#else
            UnityEngine.Microphone.End(deviceName);
#endif
        }

        public static bool IsRecording(string deviceName)
        {
#if UNITY_WEBGL
            throw new NotImplementedException();
#else
            return UnityEngine.Microphone.IsRecording(deviceName);
#endif
        }

        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq)
        {
            minFreq = 0;
            maxFreq = 0;
#if UNITY_WEBGL
            throw new NotImplementedException();
#else
            UnityEngine.Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
#endif
        }

        public static int GetPosition(string deviceName)
        {
#if UNITY_WEBGL
            throw new NotImplementedException();
#else
            return UnityEngine.Microphone.GetPosition(deviceName);
#endif
        }
    }
}

// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utilities.Audio
{
    internal interface ISampleProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetPosition(string deviceName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void GetData(AudioClip clip, float[] buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void End(string deviceName);
    }

    internal class UnitySampleProvider : ISampleProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPosition(string deviceName)
            => Microphone.GetPosition(deviceName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetData(AudioClip clip, float[] buffer)
            => clip.GetData(buffer, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End(string deviceName)
            => Microphone.End(deviceName);
    }
}

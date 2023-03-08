// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Reflection;
using UnityEngine;

namespace Utilities.Audio.Editor
{
    /// <summary>
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Audio/Bindings/AudioUtil.bindings.cs
    /// </summary>
    public static class AudioEditorUtilities
    {
        private const string UnityEditorAudioUtil = "UnityEditor.AudioUtil";

        private static Assembly UnityEditorAssembly { get; } =
            typeof(UnityEditor.AudioImporter).Assembly;

        private static Type AudioUtilClass { get; } =
            UnityEditorAssembly.GetType(UnityEditorAudioUtil);

        private static MethodInfo IsPreviewClipPlaying { get; } =
            AudioUtilClass.GetMethod(
                nameof(IsPreviewClipPlaying),
                BindingFlags.Static | BindingFlags.Public,
                null,
                Array.Empty<Type>(),
                null);

        public static bool IsPlayingPreviewClip
            => (bool)IsPreviewClipPlaying.Invoke(null, Array.Empty<object>());

        private static MethodInfo PlayPreviewClip { get; } =
            AudioUtilClass.GetMethod(
                nameof(PlayPreviewClip),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

        public static void PlayClipPreview(AudioClip clip, int position = 0, bool loop = false)
            => PlayPreviewClip.Invoke(null, new object[] { clip, position, loop });

        private static MethodInfo StopAllPreviewClips { get; } =
            AudioUtilClass.GetMethod(
                nameof(StopAllPreviewClips),
                BindingFlags.Static | BindingFlags.Public,
                null,
                Array.Empty<Type>(),
                null);

        public static void StopAllClipPreviews()
            => StopAllPreviewClips.Invoke(null, Array.Empty<object>());
    }
}

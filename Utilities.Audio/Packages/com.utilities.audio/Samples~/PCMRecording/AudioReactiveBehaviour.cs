// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Utilities.Encoder.PCM.Samples.Recording
{
    public class AudioReactiveBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Transform targetSphere;

        [SerializeField]
        private float scaleMultiplier = 10f;

        [SerializeField]
        private float smoothSpeed = 5f;

        [SerializeField]
        private float currentScale = 1f;

        [SerializeField]
        private float targetScale = 1f;

        // Called automatically by Unity’s audio system on the audio thread
        private void OnAudioFilterRead(float[] data, int channels)
        {
            // Compute the RMS value (volume level)
            var sum = 0f;
            var length = data.Length;

            for (var i = 0; i < length; i += channels)
            {
                var sample = data[i];
                sum += sample * sample;
            }

            var rms = Mathf.Sqrt(sum / (length / (float)channels));
            var volume = rms * scaleMultiplier;

            // Thread-safe way to pass data to the main thread
            targetScale = Mathf.Clamp(1f + volume, 1f, 3f);
        }

        private void Update()
        {
            // Smoothly interpolate to new scale on main thread
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * smoothSpeed);

            if (targetSphere != null)
            {
                targetSphere.localScale = Vector3.one * currentScale;
            }
        }
    }
}

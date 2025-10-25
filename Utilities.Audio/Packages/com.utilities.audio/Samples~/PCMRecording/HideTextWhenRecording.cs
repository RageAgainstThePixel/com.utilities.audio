// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using Utilities.Audio;

namespace Utilities.Encoder.PCM.Samples.Recording
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class HideTextWhenRecording : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text;

        private void OnValidate()
        {
            if (text == null)
            {
                TryGetComponent(out text);
            }
        }

        private void Update()
        {
            text.enabled = !RecordingManager.IsRecording;
        }
    }
}

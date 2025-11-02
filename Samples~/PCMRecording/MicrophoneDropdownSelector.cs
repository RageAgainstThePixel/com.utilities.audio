// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using TMPro;
using UnityEngine;
using Utilities.Audio;
using Microphone = Utilities.Audio.Microphone;

namespace Utilities.Encoder.Ogg.Samples.Recording
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class MicrophoneDropdownSelector : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown dropdown;

        private void OnValidate()
        {
            if (dropdown == null)
            {
                TryGetComponent(out dropdown);
            }
        }

        private void Awake()
        {
            OnValidate();
            RefreshDeviceList();
            dropdown.onValueChanged.AddListener(OnDeviceSelected);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RefreshDeviceList();
            }
        }

        private void OnDestroy()
        {
            dropdown.onValueChanged.RemoveListener(OnDeviceSelected);
        }

        public void RefreshDeviceList()
        {
            dropdown.ClearOptions();
            var dropdownOptions = Microphone.devices.Select(device => new TMP_Dropdown.OptionData(device)).ToList();
            dropdown.AddOptions(dropdownOptions);
        }

        private void OnDeviceSelected(int index)
        {
            if (index < 0) { return; }
            var selectedDevice = Microphone.devices[index];
            Debug.Log(selectedDevice);
            RecordingManager.DefaultRecordingDevice = selectedDevice;
        }
    }
}

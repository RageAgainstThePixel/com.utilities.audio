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

        private bool hasRefreshed;

        private void OnValidate()
        {
            if (dropdown == null)
            {
                dropdown = GetComponent<TMP_Dropdown>();
            }
        }

        private void Awake()
        {
            OnValidate();
            dropdown.onValueChanged.AddListener(OnDeviceSelected);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RefreshDeviceList();
            }
        }

        private void Update()
        {
            if (!hasRefreshed) { return; }

            if (Microphone.devices.Length != dropdown.options.Count)
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
            hasRefreshed = true;
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

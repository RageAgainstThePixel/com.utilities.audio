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
                dropdown = GetComponent<TMP_Dropdown>();
            }
        }

        private void Awake()
        {
            dropdown.onValueChanged.AddListener(OnDeviceSelected);
        }

        private void OnEnable()
        {
            RefreshDeviceList();
        }

        private void Update()
        {
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

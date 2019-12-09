using Assets.SimpleLocalization;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    public class Settings : PopupWidget
    {
        public TextMeshProUGUI masterVolumeText;
        public Slider masterVolumeSlider;
        public Toggle masterVolumeToggle;
        public TextMeshProUGUI musicVolumeText;
        public Slider musicVolumeSlider;
        public Toggle musicVolumeToggle;
        public TextMeshProUGUI sfxVolumeText;
        public Slider sfxVolumeSlider;
        public Toggle sfxVolumeToggle;

        public List<TextMeshProUGUI> muteTexts;

        protected override void Awake()
        {
            masterVolumeSlider.onValueChanged.AddListener(value =>
            {
                if (masterVolumeToggle.isOn)
                    return;

                SetMasterVolume(value);
            });

            masterVolumeToggle.onValueChanged.AddListener(value =>
            {
                if (value)
                {
                    SetMasterVolume(0.0f);
                }
                else
                {
                    SetMasterVolume(masterVolumeSlider.value);
                }
            });
        }

        public override void Show()
        {
            var muteString = LocalizationManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            base.Show();
        }

        public void Apply()
        {
            var masterVolume = masterVolumeSlider.value;
            var musicVolume = musicVolumeSlider.value;
            var sfxVolume = sfxVolumeSlider.value;


        }

        private void SetMasterVolume(float value)
        {
            AudioListener.volume = value;
            var volumeString = Mathf.Approximately(value, 0.0f) ? LocalizationManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(value * 100.0f)}%";
            masterVolumeText.text = $"{LocalizationManager.Localize("UI_MASTER_VOLUME")} : {volumeString}";
        }
    }
}

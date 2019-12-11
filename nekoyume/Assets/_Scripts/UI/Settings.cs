using Assets.SimpleLocalization;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    public class Settings : PopupWidget
    {
        public TextMeshProUGUI addressTitle;
        public TMP_InputField addressContent;
        public TextMeshProUGUI volumeMasterText;
        public Slider volumeMasterSlider;
        public Toggle volumeMasterToggle;
        public TextMeshProUGUI confirmText;
        public TextMeshProUGUI resetKeyStoreText;
        public TextMeshProUGUI resetStoreText;
        public List<TextMeshProUGUI> muteTexts;
        public Blur blur;

        #region Mono

        protected override void Awake()
        {
            volumeMasterSlider.onValueChanged.AddListener(value =>
            {
                SetVolumeMaster(value);
            });

            volumeMasterToggle.onValueChanged.AddListener(value =>
            {
                SetVolumeMasterMute(value);
            });
            confirmText.text = LocalizationManager.Localize("UI_CLOSE");
            resetStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_STORE_TITLE");
            resetKeyStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_KEYSTORE_TITLE");
        }

        #endregion

        public override void Show()
        {
            var addressString = $"{LocalizationManager.Localize("UI_YOUR_ADDRESS")}";
            addressTitle.text = addressString;
            addressContent.text = Game.Game.instance.agent.Address.ToString();

            var muteString = LocalizationManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            var settings = Nekoyume.Settings.Instance;
            UpdateSoundSettings();

            volumeMasterSlider.value = settings.VolumeMaster;
            volumeMasterToggle.isOn = settings.isVolumeMasterMuted;

            base.Show();
            blur?.Show();
        }

        public void ApplyCurrentSettings()
        {
            Nekoyume.Settings.Instance.ApplyCurrentSettings();
            Close();
        }

        public void RevertSettings()
        {
            Nekoyume.Settings.Instance.ReloadSettings();
            UpdateSoundSettings();
            Close();
        }

        public void UpdateSoundSettings()
        {
            var settings = Nekoyume.Settings.Instance;
            SetVolumeMaster(settings.VolumeMaster);
            SetVolumeMasterMute(settings.isVolumeMasterMuted);
        }

        public void CopyAddressToClipboard()
        {
            var address = Game.Game.instance.agent.Address;

            TextEditor editor = new TextEditor()
            {
                text = address.ToString()
            };
            editor.SelectAll();
            editor.Copy();
        }

        private void SetVolumeMaster(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.VolumeMaster = value;
            AudioListener.volume = settings.isVolumeMasterMuted ? 0f : settings.VolumeMaster;
            UpdateVolumeMasterText();
        }

        private void SetVolumeMasterMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeMasterMuted = value;
            AudioListener.volume = value ? 0f : settings.VolumeMaster;
            UpdateVolumeMasterText();
        }

        private void UpdateVolumeMasterText()
        {
            var volumeString = Mathf.Approximately(AudioListener.volume, 0.0f) ?
                LocalizationManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(AudioListener.volume * 100.0f)}%";
            volumeMasterText.text = $"{LocalizationManager.Localize("UI_MASTER_VOLUME")} : {volumeString}";
        }

        private void SetVolumeSfx(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.VolumeSfx = value;
        }

        private void SetVolumeSfxMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeSfxMuted = value;
        }

        public void ResetStore()
        {
            Game.Game.instance.agent.ResetStore();
        }

        public void ResetKeyStore()
        {
            Game.Game.instance.agent.ResetKeyStore();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
        }
    }
}

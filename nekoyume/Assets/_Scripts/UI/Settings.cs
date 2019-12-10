using Assets.SimpleLocalization;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Nekoyume.UI
{
    public class Settings : PopupWidget
    {
        private const string VolumeMasterKey = "SETTINGS_VOLUME_MASTER";
        private const string VolumeMasterIsMutedKey = "SETTINGS_VOLUME_MASTER_ISMUTED";
        private const string VolumeMusicKey = "SETTINGS_VOLUME_MUSIC";
        private const string VolumeMusicIsMutedKey = "SETTINGS_VOLUME_MUSIC_ISMUTED";
        private const string VolumeSfxKey = "SETTINGS_VOLUME_SFX";
        private const string VolumeSfxIsMutedKey = "SETTINGS_VOLUME_SFX_ISMUTED";

        public TextMeshProUGUI addressText;
        public TextMeshProUGUI volumeMasterText;
        public Slider volumeMasterSlider;
        public Toggle volumeMasterToggle;
        public TextMeshProUGUI confirmText;
        public TextMeshProUGUI cancelText;
        public TextMeshProUGUI resetKeyStoreText;
        public TextMeshProUGUI resetStoreText;
        public TextMeshProUGUI openLogText;
        //public TextMeshProUGUI volumeMusicText;
        //public Slider volumeMusicSlider;
        //public Toggle volumeMusicToggle;
        //public TextMeshProUGUI volumeSfxText;
        //public Slider volumeSfxSlider;
        //public Toggle volumeSfxToggle;
        public List<TextMeshProUGUI> muteTexts;

        #region Mono

        protected override void Awake()
        {
            volumeMasterSlider.onValueChanged.AddListener(value =>
            {
                SetVolumeMaster(value);
            });
            //volumeMusicSlider.onValueChanged.AddListener(value =>
            //{
            //    SetVolumeMusic(value);
            //});
            //volumeSfxSlider.onValueChanged.AddListener(value =>
            //{
            //    SetVolumeSfx(value);
            //});

            volumeMasterToggle.onValueChanged.AddListener(value =>
            {
                SetVolumeMasterMute(value);
            });
            //volumeMusicToggle.onValueChanged.AddListener(value =>
            //{
            //    SetVolumeMusicMute(value);
            //});
            //volumeSfxToggle.onValueChanged.AddListener(value =>
            //{
            //    SetVolumeSfxMute(value);
            //});
            confirmText.text = LocalizationManager.Localize("UI_SETTINGS_CONFIRM");
            cancelText.text = LocalizationManager.Localize("UI_CANCEL");
            resetStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_STORE_TITLE");
            resetKeyStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_KEYSTORE_TITLE");
            openLogText.text = LocalizationManager.Localize("UI_OPEN_LOG");
        }

        #endregion

        public override void Show()
        {
            var addressString = $"{LocalizationManager.Localize("UI_YOUR_ADDRESS")}\n<color=white>{Game.Game.instance.agent.Address}</color>";
            addressText.text = addressString;

            var muteString = LocalizationManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            var settings = Nekoyume.Settings.Instance;
            UpdateSoundSettings();

            volumeMasterSlider.value = settings.VolumeMaster;
            //volumeMusicSlider.value = settings.VolumeMusic;
            //volumeSfxSlider.value = settings.VolumeSfx;
            //volumeMasterToggle.isOn = settings.isVolumeMasterMuted;
            //volumeMusicToggle.isOn = settings.isVolumeMusicMuted;
            //volumeSfxToggle.isOn = settings.isVolumeSfxMuted;

            base.Show();
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
            SetVolumeMusic(settings.VolumeMusic);
            SetVolumeSfx(settings.VolumeSfx);
            SetVolumeMasterMute(settings.isVolumeMasterMuted);
            SetVolumeMusicMute(settings.isVolumeMusicMuted);
            SetVolumeSfxMute(settings.isVolumeSfxMuted);
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

        private void SetVolumeMusic(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.VolumeMusic = value;
            //UpdateVolumeMusicText();
        }

        private void SetVolumeMusicMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeMusicMuted = value;
            //UpdateVolumeMusicText();
        }

        //private void UpdateVolumeMusicText()
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    var volumeString = settings.isVolumeMusicMuted || Mathf.Approximately(settings.VolumeMusic, 0.0f) ?
        //        LocalizationManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(settings.VolumeMusic * 100.0f)}%";

        //    volumeMusicText.text = $"{LocalizationManager.Localize("UI_MUSIC_VOLUME")} : {volumeString}";
        //}

        private void SetVolumeSfx(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.VolumeSfx = value;
            //UpdateVolumeSfxText();
        }

        private void SetVolumeSfxMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeSfxMuted = value;
            //UpdateVolumeSfxText();
        }

        //private void UpdateVolumeSfxText()
        //{
        //    var settings = Nekoyume.Settings.Instance;
        //    var volumeString = settings.isVolumeSfxMuted || Mathf.Approximately(settings.VolumeSfx, 0.0f) ?
        //        LocalizationManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(settings.VolumeSfx * 100.0f)}%";

        //    volumeSfxText.text = $"{LocalizationManager.Localize("UI_SFX_VOLUME")} : {volumeString}";
        //}

        public void OpenLogDirectory()
        {
            var path = Path.Combine(Application.persistentDataPath, "Player.log");
            EditorUtility.RevealInFinder(path);
        }

        public void ResetStore()
        {
            Game.Game.instance.agent.ResetStore();
        }

        public void ResetKeyStore()
        {
            Game.Game.instance.agent.ResetKeyStore();
        }
    }
}

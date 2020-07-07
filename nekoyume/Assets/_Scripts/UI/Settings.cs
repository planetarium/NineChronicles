using System;
using Assets.SimpleLocalization;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Helper;
using UniRx;

namespace Nekoyume.UI
{
    public class Settings : PopupWidget
    {
        public TextMeshProUGUI addressTitleText;
        public TMP_InputField addressContentInputField;
        public Button addressCopyButton;
        public TextMeshProUGUI privateKeyTitleText;
        public TMP_InputField privateKeyContentInputField;
        public Button privateKeyCopyButton;
        public TextMeshProUGUI warningText;
        public TextMeshProUGUI volumeMasterText;
        public Slider volumeMasterSlider;
        public Toggle volumeMasterToggle;
        public List<TextMeshProUGUI> muteTexts;
        public TextMeshProUGUI resetKeyStoreText;
        public TextMeshProUGUI resetStoreText;
        public TextMeshProUGUI confirmText;
        public TextMeshProUGUI redeemCodeText;
        public Blur blur;
        public RedeemCode redeemCode;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            addressTitleText.text = LocalizationManager.Localize("UI_YOUR_ADDRESS");
            privateKeyTitleText.text = LocalizationManager.Localize("UI_YOUR_PRIVATE_KEY");
            warningText.text = LocalizationManager.Localize("UI_ACCOUNT_WARNING");

            volumeMasterSlider.onValueChanged.AddListener(SetVolumeMaster);
            volumeMasterToggle.onValueChanged.AddListener(SetVolumeMasterMute);

            resetStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_STORE_TITLE");
            resetKeyStoreText.text = LocalizationManager.Localize("UI_CONFIRM_RESET_KEYSTORE_TITLE");
            confirmText.text = LocalizationManager.Localize("UI_CLOSE");
            redeemCodeText.text = LocalizationManager.Localize("UI_REDEEM_CODE");

            addressCopyButton.OnClickAsObservable().Subscribe(_ => CopyAddressToClipboard());
            privateKeyCopyButton.OnClickAsObservable().Subscribe(_ => CopyPrivateKeyToClipboard());
            redeemCode.Close();
        }

        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            if (Game.Game.instance.Agent.PrivateKey is null)
            {
                addressContentInputField.text = string.Empty;
                privateKeyContentInputField.text = string.Empty;
            }
            else
            {
                addressContentInputField.text = Game.Game.instance.Agent.Address.ToHex();
                privateKeyContentInputField.text = ByteUtil.Hex(Game.Game.instance.Agent.PrivateKey.ByteArray);
            }

            var muteString = LocalizationManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            var settings = Nekoyume.Settings.Instance;
            UpdateSoundSettings();

            volumeMasterSlider.value = settings.volumeMaster;
            volumeMasterToggle.isOn = settings.isVolumeMasterMuted;

            base.Show(ignoreStartAnimation);

            if (blur)
            {
                blur.Show();
            }
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            HelpPopup.HelpMe(100014);
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
            SetVolumeMaster(settings.volumeMaster);
            SetVolumeMasterMute(settings.isVolumeMasterMuted);
        }

        private void CopyAddressToClipboard()
        {
            ClipboardHelper.CopyToClipboard(addressContentInputField.text);

            // todo: 복사되었습니다. 토스트.
        }

        private void CopyPrivateKeyToClipboard()
        {
            ClipboardHelper.CopyToClipboard(privateKeyContentInputField.text);

            // todo: 복사되었습니다. 토스트.
        }

        private void SetVolumeMaster(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.volumeMaster = value;
            AudioListener.volume = settings.isVolumeMasterMuted ? 0f : settings.volumeMaster;
            UpdateVolumeMasterText();
        }

        private void SetVolumeMasterMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeMasterMuted = value;
            AudioListener.volume = value ? 0f : settings.volumeMaster;
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
            settings.volumeSfx = value;
        }

        private void SetVolumeSfxMute(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isVolumeSfxMuted = value;
        }

        public void ResetStore()
        {
            Game.Game.instance.ResetStore();
        }

        public void ResetKeyStore()
        {
            Game.Game.instance.ResetKeyStore();
        }

        public void RedeemCode()
        {
            redeemCode.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }
    }
}

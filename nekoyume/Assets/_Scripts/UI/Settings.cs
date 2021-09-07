using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using UniRx;
using TimeSpan = System.TimeSpan;

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
        public Button closeButton;
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
        public Dropdown resolutionDropdown;
        public Toggle windowedToggle;

        private PrivateKey _privateKey;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            addressTitleText.text = L10nManager.Localize("UI_YOUR_ADDRESS");
            privateKeyTitleText.text = L10nManager.Localize("UI_YOUR_PRIVATE_KEY");
            warningText.text = L10nManager.Localize("UI_ACCOUNT_WARNING");

            volumeMasterSlider.onValueChanged.AddListener(SetVolumeMaster);
            volumeMasterToggle.onValueChanged.AddListener(SetVolumeMasterMute);

            resetStoreText.text = L10nManager.Localize("UI_CONFIRM_RESET_STORE_TITLE");
            resetKeyStoreText.text = L10nManager.Localize("UI_CONFIRM_RESET_KEYSTORE_TITLE");
            confirmText.text = L10nManager.Localize("UI_CLOSE");
            redeemCodeText.text = L10nManager.Localize("UI_REDEEM_CODE");

            addressCopyButton.OnClickAsObservable().Subscribe(_ => CopyAddressToClipboard())
                .AddTo(addressCopyButton);

            addressCopyButton.OnClickAsObservable().ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                    OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_COPIED")))
                .AddTo(addressCopyButton);

            privateKeyCopyButton.OnClickAsObservable().Subscribe(_ => CopyPrivateKeyToClipboard())
                .AddTo(privateKeyCopyButton);

            privateKeyCopyButton.OnClickAsObservable().ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                    OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_COPIED")))
                .AddTo(privateKeyCopyButton);

            redeemCode.OnRequested.AddListener(() =>
            {
                Close(true);
            });

            closeButton.onClick.AddListener(() =>
            {
                ApplyCurrentSettings();
                AudioController.PlayClick();
            });
            blur.button.onClick.AddListener(ApplyCurrentSettings);
            redeemCode.Close();

            InitResolution();
        }

        protected override void OnEnable()
        {
            SubmitWidget = () => Close(true);
            CloseWidget = () => Close(true);
            base.OnEnable();
        }


        void InitResolution()
        {
            var settings = Nekoyume.Settings.Instance;
            var options = settings.Resolutions.Select(resolution => $"{resolution.Width} x {resolution.Height}").ToList();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = settings.resolutionIndex;
            resolutionDropdown.RefreshShownValue();

            windowedToggle.onValueChanged.AddListener(SetWindowed);
        }
        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            if (!(_privateKey is null))
            {
                addressContentInputField.text = _privateKey.ToAddress().ToString();
                privateKeyContentInputField.text = ByteUtil.Hex(_privateKey.ByteArray);
            }
            else
            {
                if (Game.Game.instance.Agent.PrivateKey is null)
                {
                    addressContentInputField.text = string.Empty;
                    privateKeyContentInputField.text = string.Empty;
                }
                else
                {
                    addressContentInputField.text = Game.Game.instance.Agent.Address.ToString();
                    privateKeyContentInputField.text = ByteUtil.Hex(Game.Game.instance.Agent.PrivateKey.ByteArray);
                }
            }

            var muteString = L10nManager.Localize("UI_MUTE_AUDIO");
            foreach (var text in muteTexts)
            {
                text.text = muteString;
            }

            var settings = Nekoyume.Settings.Instance;
            UpdateSoundSettings();

            volumeMasterSlider.value = settings.volumeMaster;
            volumeMasterToggle.isOn = settings.isVolumeMasterMuted;
            windowedToggle.isOn = settings.isWindowed;

            base.Show(true);

            if (blur)
            {
                blur.Show();
            }
            HelpPopup.HelpMe(100014, true);
        }

        public void ApplyCurrentSettings()
        {
            Nekoyume.Settings.Instance.ApplyCurrentSettings();
            Close(true);
        }

        public void RevertSettings()
        {
            Nekoyume.Settings.Instance.ReloadSettings();
            UpdateSoundSettings();
            Close(true);
        }

        public void UpdateSoundSettings()
        {
            var settings = Nekoyume.Settings.Instance;
            SetVolumeMaster(settings.volumeMaster);
            SetVolumeMasterMute(settings.isVolumeMasterMuted);
        }

        public void UpdateResolution()
        {

        }

        public void UpdatePrivateKey(string privateKeyHex)
        {
            if (!string.IsNullOrEmpty(privateKeyHex))
            {
                _privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }
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
                L10nManager.Localize("UI_MUTE_AUDIO") : $"{Mathf.CeilToInt(AudioListener.volume * 100.0f)}%";
            volumeMasterText.text = $"{L10nManager.Localize("UI_MASTER_VOLUME")} : {volumeString}";
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

        public void SetResolution(int index)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.resolutionIndex = index;
            settings.ApplyCurrentResolution();
        }

        public void SetWindowed(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isWindowed = value;
            settings.ApplyCurrentResolution();
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
            if (blur && blur.isActiveAndEnabled)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }
    }
}

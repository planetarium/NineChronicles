using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using UniRx;
using TimeSpan = System.TimeSpan;
using Nekoyume.UI.Scroller;
using mixpanel;
using Nekoyume.State;
using Nekoyume.Blockchain;

namespace Nekoyume.UI
{
    public class SettingPopup : PopupWidget
    {
        [SerializeField]
        private GameObject addressContainer;

        [SerializeField]
        private TextMeshProUGUI addressTitleText;

        [SerializeField]
        private TMP_InputField addressContentInputField;

        [SerializeField]
        private Button addressCopyButton;

        [SerializeField]
        private TextMeshProUGUI privateKeyTitleText;

        [SerializeField]
        private TMP_InputField privateKeyContentInputField;

        [SerializeField]
        private Button privateKeyCopyButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private TextMeshProUGUI warningText;

        [SerializeField]
        private TextMeshProUGUI volumeMasterText;

        [SerializeField]
        private Slider volumeMasterSlider;

        [SerializeField]
        private Toggle volumeMasterToggle;

        [SerializeField]
        private List<TextMeshProUGUI> muteTexts;

        [SerializeField]
        private TextMeshProUGUI resetKeyStoreText;

        [SerializeField]
        private TextMeshProUGUI resetStoreText;

        [SerializeField]
        private TextMeshProUGUI confirmText;

        [SerializeField]
        private TextMeshProUGUI redeemCodeText;

        [SerializeField]
        private RedeemCode redeemCode;

        [SerializeField]
        private Dropdown resolutionDropdown;

        [SerializeField]
        private Toggle windowedToggle;


        [SerializeField]
        private Toggle pushToggle;

        [SerializeField]
        private Toggle nighttimePushToggle;

        [SerializeField]
        private Toggle rewardPushToggle;

        [SerializeField]
        private Toggle workshopPushToggle;

        [SerializeField]
        private Toggle arenaPushToggle;

        [SerializeField]
        private Toggle worldbossPushToggle;

        [SerializeField]
        private Toggle patrolRewardPushToggle;

        [SerializeField]
        private Image pushDisabledImage;

        [SerializeField]
        private Button addressShareButton;

        [SerializeField]
        private Button deleteAccountButton;

        [SerializeField]
        private List<GameObject> mobileDisabledMenus;

        [SerializeField]
        private List<GameObject> mobileEnabledMenus;

        private PrivateKey _privateKey;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            addressContainer.SetActive(!Game.LiveAsset.GameConfig.IsKoreanBuild);
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
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_COPIED"),
                        NotificationCell.NotificationType.Notification))
                .AddTo(addressCopyButton);

            addressShareButton.OnClickAsObservable().Subscribe(_ =>
            {
                var agent = Game.Game.instance.Agent;
                var cachedPassphrase = KeyManager.GetCachedPassphrase(
                    agent.Address,
                    Util.AesDecrypt,
                    defaultValue: string.Empty);
                if (cachedPassphrase.Equals(string.Empty))
                {
                    Find<LoginSystem>().ShowResetPassword();
                }
                else
                {
                    SharePrivateKeyToQRCode();
                }
            }).AddTo(addressShareButton);

            deleteAccountButton.OnClickAsObservable().Subscribe(_ =>
            {
                var confirm = Widget.Find<ConfirmPopup>();
                confirm.CloseCallback = result =>
                {
                    if (result == ConfirmResult.No)
                    {
                        return;
                    }

                    Analyzer.Instance.Track("Unity/DeleteAccount", new Dictionary<string, Value>()
                    {
                        ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                        ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                        ["SocialEmail"] = Game.Game.instance.CurrentSocialEmail
                    });
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                };
                confirm.Show("UI_SETTINGS_DELETE_ACCOUNT_TITLE", "UI_SETTINGS_DELETE_ACCOUNT_DESCRIPTION", "UI_SETTINGS_DELETE_ACCOUNT_BUTTON", "UI_SETTINGS_DELETE_ACCOUNT_CANCEL_BUTTON");
            }).AddTo(deleteAccountButton);

            privateKeyCopyButton.OnClickAsObservable().Subscribe(_ => CopyPrivateKeyToClipboard())
                .AddTo(privateKeyCopyButton);

            privateKeyCopyButton.OnClickAsObservable().ThrottleFirst(TimeSpan.FromSeconds(2f))
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_COPIED"),
                        NotificationCell.NotificationType.Notification))
                .AddTo(privateKeyCopyButton);

            redeemCode.OnRequested.AddListener(() => Close(true));
            closeButton.onClick.AddListener(() => Close());
            redeemCode.Close();

            pushToggle.onValueChanged.AddListener(SetPushEnabled);
            nighttimePushToggle.onValueChanged.AddListener(SetNightTimePush);
            rewardPushToggle.onValueChanged.AddListener(SetRewardPush);
            workshopPushToggle.onValueChanged.AddListener(SetWorkshopPush);
            arenaPushToggle.onValueChanged.AddListener(SetArenaPush);
            worldbossPushToggle.onValueChanged.AddListener(SetWorldbossPush);
            patrolRewardPushToggle.onValueChanged.AddListener(SetPatrolRewardPush);

            InitResolution();
#if UNITY_IOS
            redeemCodeText.transform.parent?.parent?.parent?.gameObject?.SetActive(false);
#endif
        }

        protected override void OnEnable()
        {
            SubmitWidget = () => Close(true);
            CloseWidget = () => Close(true);
            base.OnEnable();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Settings.Instance.ApplyCurrentSettings();
            AudioController.PlayClick();
        }

        private void InitResolution()
        {
            var settings = Nekoyume.Settings.Instance;
            var options = settings.Resolutions
                .Select(resolution => $"{resolution.Width} x {resolution.Height}").ToList();
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
                addressContentInputField.text = _privateKey.Address.ToString();
                privateKeyContentInputField.text = _privateKey.ToHexWithZeroPaddings();
            }
            else
            {
                var agent = Game.Game.instance.Agent;
                if (agent?.PrivateKey is null)
                {
                    addressContentInputField.text = string.Empty;
                    privateKeyContentInputField.text = string.Empty;
                }
                else
                {

                    addressContentInputField.text = agent.Address.ToString();
                    privateKeyContentInputField.text = agent.PrivateKey.ToHexWithZeroPaddings();
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

            pushToggle.isOn = settings.isPushEnabled;
            nighttimePushToggle.isOn = settings.isNightTimePushEnabled;
            rewardPushToggle.isOn = settings.isRewardPushEnabled;
            workshopPushToggle.isOn = settings.isWorkshopPushEnabled;
            arenaPushToggle.isOn = settings.isArenaPushEnabled;
            worldbossPushToggle.isOn = settings.isWorldbossPushEnabled;
            patrolRewardPushToggle.isOn = settings.isPatrolRewardPushEnabled;

            base.Show(true);

#if UNITY_ANDROID || UNITY_IOS
            foreach (var menu in mobileEnabledMenus)
            {
                menu.SetActive(true);
            }

            foreach (var menu in mobileDisabledMenus)
            {
                menu.SetActive(false);
            }
#else
            foreach (var menu in mobileEnabledMenus)
            {
                menu.SetActive(false);
            }

            foreach (var menu in mobileDisabledMenus)
            {
                menu.SetActive(true);
            }
#endif
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

        private void SharePrivateKeyToQRCode()
        {
            new NativeShare().AddFile(Util.GetQrCodePngFromKeystore(), "shareQRImg.png")
                .SetSubject(L10nManager.Localize("UI_SHARE_QR_TITLE"))
                .SetText(L10nManager.Localize("UI_SHARE_QR_CONTENT"))
                .Share();
        }

        private void SetVolumeMaster(float value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.volumeMaster = value;
            AudioListener.volume = settings.MasterVolume;
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
            var volumeString = Mathf.Approximately(AudioListener.volume, 0.0f)
                ? L10nManager.Localize("UI_MUTE_AUDIO")
                : $"{Mathf.CeilToInt(AudioListener.volume * 100.0f)}%";
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

        public void SetPushEnabled(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isPushEnabled = value;
            pushDisabledImage.enabled = !value;
        }

        public void SetNightTimePush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isNightTimePushEnabled = value;
        }

        public void SetRewardPush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isRewardPushEnabled = value;
        }

        public void SetWorkshopPush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isWorkshopPushEnabled = value;
        }

        public void SetArenaPush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isArenaPushEnabled = value;
        }

        public void SetWorldbossPush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isWorldbossPushEnabled = value;
        }

        public void SetPatrolRewardPush(bool value)
        {
            var settings = Nekoyume.Settings.Instance;
            settings.isPatrolRewardPushEnabled = value;
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
    }
}

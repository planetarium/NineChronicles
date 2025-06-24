#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.IO;
using System.Linq;
using Jdenticon;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Blockchain;
    using UniRx;

    public class LoginSystem : SystemWidget
    {
        public enum States
        {
            Show,
            CreateAccount,
            Login,
            FindPassphrase,
            ResetPassphrase,
            Failed,
            SetPassword,
            Login_Mobile,
            ConnectedAddress_Mobile
        }

        private static class AnalyzeCache
        {
            public static bool IsTrackedRetypePassword = false;
            public static bool IsTrackedInputPassword = false;

            public static void Reset()
            {
                IsTrackedRetypePassword = false;
                IsTrackedInputPassword = false;
            }
        }

        public GameObject header;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;

        public GameObject createSuccessGroup;

        [Space]
        public GameObject accountGroup;

        public Image accountImage;
        public TextMeshProUGUI accountAddressText;
        public TextMeshProUGUI accountAddressHolder;
        public TextMeshProUGUI accountWarningText;

        [Space]
        public GameObject passPhraseGroup;

        public TMP_InputField passPhraseField;

        [Space]
        public GameObject retypeGroup;

        public TMP_InputField retypeField;
        public TextMeshProUGUI retypeText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI incorrectText;

        [Space]
        public GameObject loginGroup;

        public TMP_InputField loginField;
        public GameObject loginWarning;

        [Space]
        public TextMeshProUGUI findPassphraseTitle;

        public GameObject findPassphraseGroup;
        public TMP_InputField findPassphraseField;
        public GameObject findPrivateKeyWarning;

        [Space]
        public ConditionalButton submitButton;

        public Button findPassphraseButton;
        public Button backToLoginButton;
        public Button setPasswordLaterButton;

        public readonly ReactiveProperty<States> State = new();

        private States _prevState;

        public override bool CanHandleInputEvent => false;

        protected override void Awake()
        {
            State.Value = States.Show;
            State.Subscribe(SubscribeState).AddTo(gameObject);

            correctText.gameObject.SetActive(false);
            incorrectText.gameObject.SetActive(false);
            submitButton.Text = L10nManager.Localize("UI_GAME_START");
            submitButton.OnSubmitSubject.Subscribe(_ => Submit()).AddTo(gameObject);
            setPasswordLaterButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/SetPassword/Cancel");
                Close(true);
            });

            passPhraseField.onEndEdit.AddListener(CheckPassphrase);
            retypeField.onEndEdit.AddListener(CheckRetypePassphrase);

            base.Awake();
            SubmitWidget = Submit;
        }

        private void SubscribeState(States states)
        {
            NcDebug.Log($"[LoginSystem] SubscribeState: {states}");
            titleText.gameObject.SetActive(true);
            contentText.gameObject.SetActive(false);

            accountGroup.SetActive(false);
            passPhraseGroup.SetActive(false);
            retypeGroup.SetActive(false);
            loginGroup.SetActive(false);
            findPassphraseTitle.gameObject.SetActive(false);
            findPassphraseGroup.SetActive(false);

            submitButton.Interactable = false;
            findPassphraseButton.gameObject.SetActive(false);
            backToLoginButton.gameObject.SetActive(false);
            setPasswordLaterButton.gameObject.SetActive(false);

            accountAddressText.gameObject.SetActive(false);
            accountAddressHolder.gameObject.SetActive(false);
            accountWarningText.gameObject.SetActive(false);
            retypeText.gameObject.SetActive(false);
            loginWarning.SetActive(false);
            findPrivateKeyWarning.SetActive(false);
            createSuccessGroup.SetActive(false);

            switch (states)
            {
                case States.Show:
                    header.SetActive(true);
                    contentText.gameObject.SetActive(true);
                    contentText.text = L10nManager.Localize("UI_LOGIN_SHOW_CONTENT");
                    accountGroup.SetActive(true);
                    accountAddressHolder.gameObject.SetActive(true);
                    submitButton.Text = L10nManager.Localize("UI_GAME_SIGN_UP");
                    break;
                case States.SetPassword:
                    titleText.text = L10nManager.Localize("UI_SET_PASSWORD_TITLE");
                    submitButton.Text = L10nManager.Localize("UI_CONFIRM");
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    setPasswordLaterButton.gameObject.SetActive(true);
                    break;
                case States.CreateAccount:
                    titleText.gameObject.SetActive(false);
                    submitButton.Text = L10nManager.Localize("UI_GAME_CREATE_PASSWORD");
                    createSuccessGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.ResetPassphrase:
                    titleText.gameObject.SetActive(false);
                    submitButton.Text = L10nManager.Localize("UI_GAME_START");
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.Login:
                    header.SetActive(false);
                    titleText.gameObject.SetActive(false);
                    submitButton.Text = L10nManager.Localize("UI_GAME_START");
                    loginGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    findPassphraseButton.gameObject.SetActive(false);
                    loginField.Select();
                    accountAddressText.gameObject.SetActive(true);
                    break;
                case States.Login_Mobile:
                    header.SetActive(false);
                    titleText.gameObject.SetActive(false);
                    submitButton.Text = L10nManager.Localize("UI_GAME_START");
                    loginGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    findPassphraseButton.gameObject.SetActive(false);
                    accountAddressText.gameObject.SetActive(true);
                    break;
                case States.FindPassphrase:
                    titleText.gameObject.SetActive(false);
                    findPassphraseTitle.gameObject.SetActive(true);
                    findPassphraseGroup.SetActive(true);
                    backToLoginButton.gameObject.SetActive(true);
                    submitButton.Text = L10nManager.Localize("UI_OK");
                    findPassphraseField.Select();
                    break;
                case States.ConnectedAddress_Mobile:
                    accountGroup.SetActive(true);
                    contentText.gameObject.SetActive(true);
                    contentText.text = L10nManager.Localize("UI_CONNECTED_ADDRESS_CONTENT");
                    submitButton.Text = L10nManager.Localize("UI_IMPORT");
                    break;
                case States.Failed:
                    var upper = _prevState.ToString().ToUpper();
                    var format = L10nManager.Localize($"UI_LOGIN_{upper}_FAIL");
                    titleText.text = string.Format(format, _prevState);
                    contentText.gameObject.SetActive(true);
                    var contentFormat = L10nManager.Localize($"UI_LOGIN_{upper}_CONTENT");
                    contentText.text = string.Format(contentFormat);
                    submitButton.Text = L10nManager.Localize("UI_OK");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(states), states, null);
            }

            UpdateSubmitButton();
        }

        public void CheckPassphrase(string text)
        {
            if (!AnalyzeCache.IsTrackedInputPassword)
            {
                AnalyzeCache.IsTrackedInputPassword = true;
                Analyzer.Instance.Track("Unity/Login/Password/Input");
            }

            var valid = submitButton.IsSubmittable;
            correctText.gameObject.SetActive(valid);
            incorrectText.gameObject.SetActive(!valid && !string.IsNullOrEmpty(retypeField.text));
            retypeField.interactable = true;
        }

        public void CheckRetypePassphrase(string text)
        {
            if (!AnalyzeCache.IsTrackedRetypePassword)
            {
                AnalyzeCache.IsTrackedRetypePassword = true;
                Analyzer.Instance.Track("Unity/Login/Password/Retype");
            }

            UpdateSubmitButton();
            var valid = submitButton.IsSubmittable;
            correctText.gameObject.SetActive(valid);
            incorrectText.gameObject.SetActive(!valid);
            retypeText.gameObject.SetActive(!valid);
        }

        private bool CheckPasswordValidInCreate()
        {
            var passPhrase = passPhraseField.text;
            var retyped = retypeField.text;
            return !(string.IsNullOrEmpty(passPhrase) || string.IsNullOrEmpty(retyped)) &&
                passPhrase == retyped;
        }

        private void CheckLogin(System.Action success)
        {
            NcDebug.Log($"[LoginSystem] CheckLogin invoked");
            if (!KeyManager.Instance.TrySigninWithTheFirstRegisteredKey(loginField.text))
            {
                loginWarning.SetActive(true);
                return;
            }

            if (KeyManager.Instance.IsSignedIn)
            {
                NcDebug.Log($"[LoginSystem] CheckLogin... success");
                if (Platform.IsMobilePlatform())
                {
                    NcDebug.Log($"[LoginSystem] CheckLogin... cache passphrase");
                    KeyManager.Instance.CachePassphrase(
                        KeyManager.Instance.SignedInAddress,
                        loginField.text);
                }

                success?.Invoke();
            }
            else
            {
                loginWarning.SetActive(true);
                loginField.text = string.Empty;
            }
        }

        public void Submit()
        {
            NcDebug.Log($"[LoginSystem] Submit invoked: submittable({submitButton.IsSubmittable})" +
                $", {State.Value}");
            if (!submitButton.IsSubmittable)
            {
                return;
            }

            Analyzer.Instance.Track("Unity/Login/GameStartButton/Click");

            submitButton.Interactable = false;
            switch (State.Value)
            {
                case States.Show:
                    KeyManager.Instance.SignIn(new PrivateKey());
                    SetState(States.CreateAccount);
                    SetImage(KeyManager.Instance.SignedInAddress);
                    break;
                case States.CreateAccount:
                    KeyManager.Instance.SignInAndRegister(
                        KeyManager.Instance.SignedInPrivateKey,
                        passPhraseField.text);
                    Close();
                    break;
                case States.SetPassword:
                    KeyManager.Instance.SignInAndRegister(
                        KeyManager.Instance.SignedInPrivateKey,
                        passPhraseField.text,
                        true);
                    KeyManager.Instance.CachePassphrase(
                        KeyManager.Instance.SignedInAddress,
                        passPhraseField.text);
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_SET_PASSWORD_COMPLETE"), NotificationCell.NotificationType.Notification);
                    Analyzer.Instance.Track("Unity/SetPassword/Complete");

#if RUN_ON_MOBILE
                    new NativeShare().AddFile(Util.GetQrCodePngFromKeystore(), "shareQRImg.png")
                        .SetSubject(L10nManager.Localize("UI_SHARE_QR_TITLE"))
                        .SetText(L10nManager.Localize("UI_SHARE_QR_CONTENT"))
                        .Share();
#endif
                    Close();
                    break;
                case States.Login:
                    CheckLogin(() => Close());
                    break;
                case States.FindPassphrase:
                    if (KeyManager.Instance.Has(findPassphraseField.text))
                    {
                        SetState(States.ResetPassphrase);
                    }
                    else
                    {
                        findPrivateKeyWarning.SetActive(true);
                        findPassphraseField.text = null;
                    }

                    break;
                case States.ResetPassphrase:
                    KeyManager.Instance.SignInAndRegister(
                        findPassphraseField.text,
                        passPhraseField.text,
                        true);
                    Close();
                    break;
                case States.Failed:
                    SetState(_prevState);
                    break;
                case States.Login_Mobile:
                    CheckLogin(() => Close());
                    break;
                case States.ConnectedAddress_Mobile:
                    Find<IntroScreen>().ShowForQrCodeGuide();
                    Close();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void FindPassphrase()
        {
            SetState(States.FindPassphrase);
        }

        public void BackToLogin()
        {
            SetState(States.Login);
        }

        public void Show(string privateKeyString)
        {
            // WARNING: Do not log privateKeyString.
            NcDebug.Log("[LoginSystem] Show(string) invoked with " +
                $"privateKeyString({(privateKeyString is null ? "null" : "not null")}).");
            AnalyzeCache.Reset();

            //Auto login for miner, seed, launcher
            if (!string.IsNullOrEmpty(privateKeyString) || Application.isBatchMode)
            {
                CreatePrivateKey(privateKeyString);
                Close();

                return;
            }

#if RUN_ON_MOBILE
            // 해당 함수를 호출했을 때에 유효한 Keystore가 있는 것을 기대하고 있음
            SetState(States.Login_Mobile);
            SetImage(KeyManager.Instance.GetList().First().Item2.Address);
#else
            var state = KeyManager.Instance.GetList().Any()
                ? States.Login
                : States.Show;
            SetState(state);
            if (state == States.Login)
            {
                // 키 고르는 게 따로 없으니 갖고 있는 키 중에서 아무거나 보여줘야 함...
                // FIXME: 역시 키 고르는 단계가 있어야 할 것 같음
                SetImage(KeyManager.Instance.GetList().First().Item2.Address);
            }

            switch (State.Value)
            {
                case States.CreateAccount:
                case States.ResetPassphrase:
                case States.SetPassword:
                {
                    {
                        if (passPhraseField.isFocused)
                        {
                            retypeField.Select();
                        }
                        else
                        {
                            passPhraseField.Select();
                        }
                    }
                    break;
                }
                case States.Login:
                    loginField.Select();
                    break;
                case States.FindPassphrase:
                    findPassphraseField.Select();
                    break;
                case States.Show:
                case States.Failed:
                    break;
            }
#endif
            base.Show();
        }

        // Keystore 가 없을 때에만 가능해야 함
        public void Show(Address? connectedAddress)
        {
            NcDebug.Log($"[LoginSystem] Show invoked: connectedAddress({connectedAddress})");
            // accountExist
            if (connectedAddress.HasValue)
            {
                Analyzer.Instance.Track("Unity/Login/1");

                SetState(States.ConnectedAddress_Mobile);
                SetImage(connectedAddress.Value);
            }
            else
            {
                Analyzer.Instance.Track("Unity/Login/2");

                KeyManager.Instance.SignInAndRegister(new PrivateKey(), passPhraseField.text);
                Close();
                return;
            }

            base.Show();
        }

        public void ShowResetPassword()
        {
            NcDebug.Log($"[LoginSystem] ShowResetPassword invoked");
            Analyzer.Instance.Track("Unity/SetPassword/Show");

            SetState(States.SetPassword);
            base.Show();
        }

        private void CreatePrivateKey(string privateKeyString)
        {
            if (string.IsNullOrEmpty(privateKeyString))
            {
                if (KeyManager.Instance.TrySigninWithTheFirstRegisteredKey(passPhraseField.text))
                {
                    return;
                }

                KeyManager.Instance.SignInAndRegister(new PrivateKey(), passPhraseField.text);
            }
            else
            {
                KeyManager.Instance.SignInAndRegister(
                    new PrivateKey(ByteUtil.ParseHex(privateKeyString)),
                    passPhraseField.text);
                NcDebug.LogWarningFormat(
                    "As --private-key option is used, keystore files are ignored.\n" +
                    "Loaded key (address): {0}",
                    KeyManager.Instance.SignedInAddress
                );
            }
        }

        private void UpdateSubmitButton()
        {
            submitButton.Interactable = State.Value switch
            {
                States.Show => true,
                States.CreateAccount => true,
                States.Failed => true,
                States.ConnectedAddress_Mobile => true,
                States.Login => !string.IsNullOrEmpty(loginField.text),
                States.FindPassphrase => !string.IsNullOrEmpty(findPassphraseField.text),
                States.Login_Mobile => !string.IsNullOrEmpty(loginField.text),
                States.ResetPassphrase => CheckPasswordValidInCreate(),
                States.SetPassword => CheckPasswordValidInCreate(),
                _ => false
            };
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                switch (State.Value)
                {
                    case States.ResetPassphrase:
                    case States.SetPassword:
                        if (passPhraseField.isFocused)
                        {
                            retypeField.Select();
                        }
                        else
                        {
                            passPhraseField.Select();
                        }

                        break;
                    case States.Login:
                    case States.Login_Mobile:
                        loginField.Select();
                        break;
                    case States.FindPassphrase:
                        findPassphraseField.Select();
                        break;
                    case States.CreateAccount:
                    case States.Show:
                    case States.Failed:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            UpdateSubmitButton();
        }

        private void SetState(States states)
        {
            _prevState = State.Value;
            State.Value = states;
        }

        private void SetImage(Address address)
        {
            var image = Identicon.FromValue(address, 62);
            var bgColor = image.Style.BackColor;
            image.Style.BackColor = Jdenticon.Rendering.Color.FromRgba(bgColor.R, bgColor.G, bgColor.B, 0);
            var ms = new MemoryStream();
            image.SaveAsPng(ms);
            var buffer = new byte[ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            var t = new Texture2D(8, 8);
            if (t.LoadImage(ms.ToArray()))
            {
                var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                accountImage.overrideSprite = sprite;
                accountImage.SetNativeSize();
                accountAddressText.text = address.ToString();
                accountAddressText.gameObject.SetActive(true);
            }
        }
    }
}

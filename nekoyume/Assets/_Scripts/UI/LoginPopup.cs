using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.SimpleLocalization;
using Jdenticon;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.EnumType;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoginPopup : Widget
    {
        public enum States
        {
            Show,
            CreateAccount,
            Login,
            FindPassphrase,
            ResetPassphrase,
            Failed,
            CreatePassword,
        }

        public override WidgetType WidgetType => WidgetType.SystemInfo;
        public InputField passPhraseField;
        public InputField retypeField;
        public InputField loginField;
        public InputField findPassphraseField;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        public GameObject passPhraseGroup;
        public GameObject retypeGroup;
        public GameObject loginGroup;
        public GameObject findPassphraseGroup;
        public GameObject accountGroup;
        public GameObject header;
        public GameObject bg;
        public GameObject loginWarning;
        public GameObject findPrivateKeyWarning;
        public GameObject createSuccessGroup;
        public TextMeshProUGUI strongText;
        public TextMeshProUGUI weakText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI incorrectText;
        public TextMeshProUGUI findPassphraseText;
        public TextMeshProUGUI backToLoginText;
        public TextMeshProUGUI passPhraseText;
        public TextMeshProUGUI retypeText;
        public TextMeshProUGUI loginText;
        public TextMeshProUGUI enterPrivateKeyText;
        public TextMeshProUGUI accountAddressText;
        public TextMeshProUGUI accountAddressHolder;
        public TextMeshProUGUI accountWarningText;
        public TextMeshProUGUI successText;
        public SubmitButton submitButton;
        public Button findPassphraseButton;
        public Button backToLoginButton;
        public TextMeshProUGUI submitText;
        public Image accountImage;
        public readonly ReactiveProperty<States> State = new ReactiveProperty<States>();
        public bool Login { get; private set; }
        private string _keyStorePath;
        private string _privateKeyString;
        private PrivateKey _privateKey;
        private States _prevState;
        private Dictionary<string, ProtectedPrivateKey> _protectedPrivateKeys = new Dictionary<string, ProtectedPrivateKey>();
        public Blur blur;

        protected override void Awake()
        {
            State.Value = States.Show;
            State.Subscribe(SubscribeState).AddTo(gameObject);
            strongText.gameObject.SetActive(false);
            weakText.gameObject.SetActive(false);
            correctText.gameObject.SetActive(false);
            incorrectText.gameObject.SetActive(false);
            contentText.text = LocalizationManager.Localize("UI_LOGIN_CONTENT");
            findPassphraseText.text = LocalizationManager.Localize("UI_LOGIN_FIND_PASSPHRASE");
            backToLoginText.text = LocalizationManager.Localize("UI_LOGIN_BACK_TO_LOGIN");
            passPhraseText.text = LocalizationManager.Localize("UI_LOGIN_PASSWORD_INFO");
            retypeText.text = LocalizationManager.Localize("UI_LOGIN_RETYPE_INFO");
            loginText.text = LocalizationManager.Localize("UI_LOGIN_INFO");
            enterPrivateKeyText.text = LocalizationManager.Localize("UI_LOGIN_PRIVATE_KEY_INFO");
            successText.text = LocalizationManager.Localize("UI_ID_CREATE_SUCCESS");
            passPhraseField.placeholder.GetComponent<Text>().text =
                LocalizationManager.Localize("UI_LOGIN_INPUT_PASSPHRASE");
            retypeField.placeholder.GetComponent<Text>().text =
                LocalizationManager.Localize("UI_LOGIN_RETYPE_PASSPHRASE");
            loginField.placeholder.GetComponent<Text>().text =
                LocalizationManager.Localize("UI_LOGIN_LOGIN");
            findPassphraseField.placeholder.GetComponent<Text>().text =
                LocalizationManager.Localize("UI_LOGIN_ENTER_PRIVATE_KEY");
            submitText.text = LocalizationManager.Localize("UI_GAME_START");
            submitButton.OnSubmitClick.Subscribe(_ => Submit());
            base.Awake();

            SubmitWidget = Submit;
        }
        private void SubscribeState(States states)
        {
            titleText.gameObject.SetActive(true);
            contentText.gameObject.SetActive(false);
            passPhraseGroup.SetActive(false);
            retypeGroup.SetActive(false);
            loginGroup.SetActive(false);
            findPassphraseGroup.SetActive(false);
            accountGroup.SetActive(false);
            submitButton.SetSubmittable(false);
            findPassphraseButton.gameObject.SetActive(false);
            backToLoginButton.gameObject.SetActive(false);
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
                    incorrectText.gameObject.SetActive(false);
                    correctText.gameObject.SetActive(false);
                    strongText.gameObject.SetActive(false);
                    weakText.gameObject.SetActive(false);
                    accountGroup.SetActive(true);
                    accountAddressHolder.gameObject.SetActive(true);
                    passPhraseField.text = "";
                    retypeField.text = "";
                    loginField.text = "";
                    findPassphraseField.text = "";
                    submitButton.SetSubmittable(true);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_GAME_SIGN_UP"));
                    bg.SetActive(false);
                    break;
                case States.CreatePassword:
                    titleText.gameObject.SetActive(false);
                    accountAddressText.gameObject.SetActive(true);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_GAME_START"));
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.CreateAccount:
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmittable(true);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_GAME_CREATE_PASSWORD"));
                    createSuccessGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.ResetPassphrase:
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_GAME_START"));
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.Login:
                    header.SetActive(false);
                    submitButton.SetSubmittable(true);
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_GAME_START"));
                    loginGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    findPassphraseButton.gameObject.SetActive(true);
                    loginField.Select();
                    accountAddressText.gameObject.SetActive(true);
                    bg.SetActive(true);
                    break;
                case States.FindPassphrase:
                    titleText.gameObject.SetActive(false);
                    findPassphraseGroup.SetActive(true);
                    backToLoginButton.gameObject.SetActive(true);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_OK"));
                    findPassphraseField.Select();
                    break;
                case States.Failed:
                    var upper = _prevState.ToString().ToUpper();
                    var format = LocalizationManager.Localize($"UI_LOGIN_{upper}_FAIL");
                    titleText.text = string.Format(format, _prevState);
                    contentText.gameObject.SetActive(true);
                    var contentFormat = LocalizationManager.Localize($"UI_LOGIN_{upper}_CONTENT");
                    contentText.text = string.Format(contentFormat);
                    submitButton.SetSubmitText(LocalizationManager.Localize("UI_OK"));
                    submitButton.SetSubmittable(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(states), states, null);
            }
        }

        public void CheckPassphrase()
        {
            var text = passPhraseField.text;
            var strong = CheckPassWord(text);
            strongText.gameObject.SetActive(strong);
            weakText.gameObject.SetActive(!strong);
            passPhraseText.gameObject.SetActive(!strong);
            retypeField.interactable = strong;
        }

        private static bool CheckPassWord(string text)
        {
            var result = Zxcvbn.Zxcvbn.MatchPassword(text);
            return result.Score >= 2 && !Regex.IsMatch(text, GameConfig.UnicodePattern);
        }

        public void CheckRetypePassphrase()
        {

            var text = passPhraseField.text;
            var same = text == retypeField.text && CheckPassWord(text);
            submitButton.SetSubmittable(same);
            correctText.gameObject.SetActive(same);
            incorrectText.gameObject.SetActive(!same);
            retypeText.gameObject.SetActive(!same);
        }

        private void CheckLogin()
        {
            try
            {
                _privateKey = CheckPrivateKey(GetProtectedPrivateKeys(), loginField.text);
            }
            catch (Exception)
            {
                loginWarning.SetActive(true);
                return;
            }
            Login = !(_privateKey is null);
            if (Login)
            {
                Close();
            }
            else
            {
                loginWarning.SetActive(true);
            }

        }

        public void Submit()
        {
            if (!submitButton.button.interactable)
            {
                return;
            }

            submitButton.SetSubmittable(false);
            switch (State.Value)
            {
                case States.Show:
                    SetState(States.CreateAccount);
                    _privateKey = new PrivateKey();
                    SetImage(_privateKey.PublicKey.ToAddress());
                    break;
                case States.CreateAccount:
                    SetState(States.CreatePassword);
                    break;
                case States.CreatePassword:
                    CreateProtectedPrivateKey(_privateKey);
                    Login = !(_privateKey is null);
                    Close();
                    break;
                case States.Login:
                    CheckLogin();
                    break;
                case States.FindPassphrase:
                {
                    if (CheckPrivateKeyHex())
                    {
                        SetState(States.ResetPassphrase);
                    }
                    else
                    {
                        findPrivateKeyWarning.SetActive(true);
                    }
                    break;
                }
                case States.ResetPassphrase:
                    ResetPassphrase();
                    Login = !(_privateKey is null);
                    Close();
                    break;
                case States.Failed:
                    SetState(_prevState);
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

        public void Show(string path, string privateKeyString)
        {
            base.Show();
            blur?.Show();

            _keyStorePath = path;
            _privateKeyString = privateKeyString;
            //Auto login for miner, seed
            if (!string.IsNullOrEmpty(_privateKeyString) || Application.isBatchMode)
            {
                CreatePrivateKey();
                Login = true;
                Close();
            }
            else
            {
                var state = GetProtectedPrivateKeys().Any() ? States.Login : States.Show;
                SetState(state);
                Login = false;
            }
        }

        private void CreatePrivateKey()
        {
            PrivateKey privateKey = null;

            if (string.IsNullOrEmpty(_privateKeyString))
            {
                var protectedPrivateKeys = GetProtectedPrivateKeys();
                privateKey = CheckPrivateKey(protectedPrivateKeys, passPhraseField.text);
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(_privateKeyString));
                Debug.LogWarningFormat(
                    "As --private-key option is used, keystore files are ignored.\n" +
                    "Loaded key (address): {0}",
                    privateKey.PublicKey.ToAddress()
                );
            }

            if (privateKey is null)
            {
                privateKey = new PrivateKey();
                CreateProtectedPrivateKey(privateKey);
            }

            _privateKey = privateKey;
        }

        private Dictionary<string, ProtectedPrivateKey> GetProtectedPrivateKeys()
        {
            if (_protectedPrivateKeys.Any())
            {
                return _protectedPrivateKeys;
            }

            if (!Directory.Exists(_keyStorePath))
            {
                Directory.CreateDirectory(_keyStorePath);
            }

            var keyPaths = Directory.EnumerateFiles(_keyStorePath);

            var protectedPrivateKeys = new Dictionary<string, ProtectedPrivateKey>();
            foreach (var keyPath in keyPaths)
            {
                if (Path.GetFileName(keyPath) is string f && f.StartsWith("."))
                {
                    continue;
                }

                using (Stream stream = new FileStream(keyPath, FileMode.Open))
                using (var reader = new StreamReader(stream))
                {
                    try
                    {
                        protectedPrivateKeys[keyPath] = ProtectedPrivateKey.FromJson(reader.ReadToEnd());
                        SetImage(protectedPrivateKeys[keyPath].Address);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarningFormat("The key file {0} is invalid: {1}", keyPath, e);
                    }
                }
            }

            Debug.LogFormat(
                "Loaded {0} protected keys in the keystore:\n{1}",
                protectedPrivateKeys.Count,
                string.Join("\n", protectedPrivateKeys.Select(kv => $"- {kv.Value}: {kv.Key}"))
            );

            // FIXME: 키가 여러 개 있을 수 있으므로 UI에서 목록으로 표시하고 유저가 선택하게 해야 함.
            _protectedPrivateKeys = protectedPrivateKeys;
            return protectedPrivateKeys;
        }

        private static PrivateKey CheckPrivateKey(Dictionary<string, ProtectedPrivateKey> protectedPrivateKeys,
            string passphrase)
        {
            PrivateKey privateKey = null;
            foreach (var kv in protectedPrivateKeys)
            {
                try
                {
                    privateKey = kv.Value.Unprotect(passphrase: passphrase);
                    // FIXME: passphrase 제대로 UI 통해서 입력 받아야 함 -^
                }
                catch (IncorrectPassphraseException)
                {
                    Debug.LogWarningFormat(
                        "The key file {0} is protected with a passphrase; failed to load: {1}",
                        kv.Value.Address,
                        kv.Key
                    );
                }

                Debug.LogFormat(
                    "The key file {0} was successfully loaded using passphrase: {1}",
                    kv.Value.Address, kv.Key
                );
                break;
            }

            return privateKey;
        }

        public PrivateKey GetPrivateKey()
        {
            return _privateKey;
        }

        protected override void Update()
        {
            base.Update();
            
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                switch (State.Value)
                {
                    case States.CreateAccount:
                    case States.ResetPassphrase:
                    case States.CreatePassword:
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
            }
        }

        private bool CheckPrivateKeyHex()
        {
            var hex = findPassphraseField.text;
            try
            {
                var pk = new PrivateKey(ByteUtil.ParseHex(hex));
                return GetProtectedPrivateKeys().Select(kv => kv.Value)
                    .Any(ppk => ppk.Address == pk.PublicKey.ToAddress());

            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ResetPassphrase()
        {
            var hex = findPassphraseField.text;
            var pk = new PrivateKey(ByteUtil.ParseHex(hex));
            var protectedPrivateKeys = GetProtectedPrivateKeys();
            var protectedPrivateKey = protectedPrivateKeys.First(i => i.Value.Address == pk.PublicKey.ToAddress());
            var path = Path.Combine(_keyStorePath, protectedPrivateKey.Key);
            if (File.Exists(path))
            {
                File.Delete(path);
                _protectedPrivateKeys.Remove(protectedPrivateKey.Key);
            }

            CreateProtectedPrivateKey(pk);
        }

        private void CreateProtectedPrivateKey(PrivateKey privateKey)
        {
            var ppk = ProtectedPrivateKey.Protect(privateKey, passPhraseField.text);
            // FIXME: passphrase 제대로 UI 통해서 입력 받아야 함. --------------------^

            var keyId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var keyPath = Path.Combine(
                _keyStorePath,
                $"UTC--{now:yyyy-MM-dd}T{now:HH-mm-ss}Z--{keyId:D}"
            );
            using (Stream f = new FileStream(keyPath, FileMode.CreateNew))
            {
                ppk.WriteJson(f, keyId);
            }

            Debug.LogFormat(
                "As there hadn't been any key file, a new key file was created ({0}): {1}",
                ppk.Address,
                keyPath
            );
            _privateKey = privateKey;
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
            ms.Read(buffer,0,buffer.Length);
            var t = new Texture2D(8,8);
            if (t.LoadImage(ms.ToArray()))
            {
                var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                accountImage.overrideSprite = sprite;
                accountImage.SetNativeSize();
                accountAddressText.text = address.ToString();
                accountAddressText.gameObject.SetActive(true);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
        }
    }
}

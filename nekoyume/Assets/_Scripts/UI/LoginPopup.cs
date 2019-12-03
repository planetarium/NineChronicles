using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.EnumType;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoginPopup : Widget
    {
        private enum State
        {
            Show,
            SignUp,
            Login,
            FindPassphrase,
            ResetPassphrase,
            Failed,
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
        public TextMeshProUGUI strongText;
        public TextMeshProUGUI weakText;
        public TextMeshProUGUI correctText;
        public TextMeshProUGUI incorrectText;
        public Button submitButton;
        public Button findPassphraseButton;
        public Button backToLoginButton;
        public TextMeshProUGUI submitText;
        private readonly ReactiveProperty<State> _state = new ReactiveProperty<State>();
        public bool Login { get; private set; }
        private string _keyStorePath;
        private string _privateKeyString;
        private PrivateKey _privateKey;
        private State _prevState;
        private Dictionary<string, ProtectedPrivateKey> _protectedPrivateKeys = new Dictionary<string, ProtectedPrivateKey>();

        protected override void Awake()
        {
            _state.Value = State.Show;
            _state.Subscribe(SubscribeState).AddTo(gameObject);

            base.Awake();
        }

        private void SubscribeState(State state)
        {
            contentText.gameObject.SetActive(false);
            passPhraseGroup.SetActive(false);
            retypeGroup.SetActive(false);
            loginGroup.SetActive(false);
            findPassphraseGroup.SetActive(false);
            submitButton.interactable = false;
            findPassphraseButton.gameObject.SetActive(false);
            backToLoginButton.gameObject.SetActive(false);
            titleText.gameObject.SetActive(true);

            switch (state)
            {
                case State.Show:
                    contentText.gameObject.SetActive(true);
                    submitButton.interactable = true;
                    break;
                case State.SignUp:
                case State.ResetPassphrase:
                    titleText.text = "Your account";
                    submitText.text = "Game Start";
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case State.Login:
                    titleText.text = "Your account";
                    submitText.text = "Game Start";
                    loginGroup.SetActive(true);
                    findPassphraseButton.gameObject.SetActive(true);
                    loginField.Select();
                    break;
                case State.FindPassphrase:
                    titleText.gameObject.SetActive(false);
                    findPassphraseGroup.SetActive(true);
                    backToLoginButton.gameObject.SetActive(true);
                    submitText.text = "Enter";
                    findPassphraseField.Select();
                    break;
                case State.Failed:
                    titleText.text = "Failed";
                    contentText.gameObject.SetActive(true);
                    contentText.text = _prevState.ToString();
                    submitText.text = "OK";
                    submitButton.interactable = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public void CheckPassphrase()
        {
            var result = Zxcvbn.Zxcvbn.MatchPassword(passPhraseField.text);
            var strong = result.Score >= 2;
            strongText.gameObject.SetActive(strong);
            weakText.gameObject.SetActive(!strong);
        }

        public void CheckRetypePassphrase()
        {
            var result = Zxcvbn.Zxcvbn.MatchPassword(passPhraseField.text);
            var strong = result.Score >= 2;
            var same = passPhraseField.text == retypeField.text && strong;
            submitButton.interactable = same;
            correctText.gameObject.SetActive(same);
            incorrectText.gameObject.SetActive(!same);
        }

        private void CheckLogin()
        {
            _privateKey = CheckPrivateKey(GetProtectedPrivateKeys(), loginField.text);
            Login = !(_privateKey is null);
            if (Login)
            {
                Close();
            }
            else
            {
                SetState(State.Failed);
            }

        }

        public void Submit()
        {
            submitButton.interactable = false;
            switch (_state.Value)
            {
                case State.Show:
                    SetState(State.SignUp);
                    break;
                case State.SignUp:
                    CreatePrivateKey();
                    Login = !(_privateKey is null);
                    Close();
                    break;
                case State.Login:
                    CheckLogin();
                    break;
                case State.FindPassphrase:
                {
                    var state = CheckPrivateKeyHex() ? State.ResetPassphrase : State.Failed;
                    SetState(state);
                    break;
                }
                case State.ResetPassphrase:
                    ResetPassphrase();
                    Login = !(_privateKey is null);
                    Close();
                    break;
                case State.Failed:
                    SetState(_prevState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void FindPassphrase()
        {
            SetState(State.FindPassphrase);
        }

        public void BackToLogin()
        {
            SetState(State.Login);
        }

        public void Show(string path, string privateKeyString)
        {
            base.Show();

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
                var state = GetProtectedPrivateKeys().Any() ? State.Login : State.Show;
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
            Debug.Log(ByteUtil.Hex(privateKey.ByteArray));

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

        private void Update()
        {
            if (_state.Value == State.SignUp)
            {
                if (Input.GetKeyUp(KeyCode.Tab))
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

        private void SetState(State state)
        {
            _prevState = _state.Value;
            _state.Value = state;
        }
    }
}

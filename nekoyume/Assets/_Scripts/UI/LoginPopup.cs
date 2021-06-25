using System;
using System.IO;
using System.Linq;
using Jdenticon;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

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

        public IKeyStore KeyStore = Web3KeyStore.DefaultKeyStore;

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
        public TextMeshProUGUI findPassphraseTitle;
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
        private string _privateKeyString;
        private PrivateKey _privateKey;
        private States _prevState;
        public Blur blur;

        protected override void Awake()
        {
            State.Value = States.Show;
            State.Subscribe(SubscribeState).AddTo(gameObject);
            strongText.gameObject.SetActive(false);
            weakText.gameObject.SetActive(false);
            correctText.gameObject.SetActive(false);
            incorrectText.gameObject.SetActive(false);
            contentText.text = L10nManager.Localize("UI_LOGIN_CONTENT");
            accountAddressHolder.text = L10nManager.Localize("UI_ACCOUNT_PLACEHOLDERS");
            findPassphraseTitle.text = L10nManager.Localize("UI_LOGIN_FIND_PASSPHRASE_TITLE");
            findPassphraseText.text = L10nManager.Localize("UI_LOGIN_FIND_PASSPHRASE");
            backToLoginText.text = L10nManager.Localize("UI_LOGIN_BACK_TO_LOGIN");
            passPhraseText.text = L10nManager.Localize("UI_LOGIN_PASSWORD_INFO");
            retypeText.text = L10nManager.Localize("UI_LOGIN_RETYPE_INFO");
            loginText.text = L10nManager.Localize("UI_LOGIN_INFO");
            enterPrivateKeyText.text = L10nManager.Localize("UI_LOGIN_PRIVATE_KEY_INFO");
            successText.text = L10nManager.Localize("UI_ID_CREATE_SUCCESS");
            passPhraseField.placeholder.GetComponent<Text>().text =
                L10nManager.Localize("UI_LOGIN_INPUT_PASSPHRASE");
            retypeField.placeholder.GetComponent<Text>().text =
                L10nManager.Localize("UI_LOGIN_RETYPE_PASSPHRASE");
            loginField.placeholder.GetComponent<Text>().text =
                L10nManager.Localize("UI_LOGIN_LOGIN");
            findPassphraseField.placeholder.GetComponent<Text>().text =
                L10nManager.Localize("UI_LOGIN_ENTER_PRIVATE_KEY");
            submitText.text = L10nManager.Localize("UI_GAME_START");
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
            findPassphraseTitle.gameObject.SetActive(false);
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
                    submitButton.SetSubmitText(L10nManager.Localize("UI_GAME_SIGN_UP"));
                    bg.SetActive(false);
                    break;
                case States.CreatePassword:
                    titleText.gameObject.SetActive(false);
                    accountAddressText.gameObject.SetActive(true);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_GAME_START"));
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.CreateAccount:
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_GAME_CREATE_PASSWORD"));
                    createSuccessGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.ResetPassphrase:
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_GAME_START"));
                    passPhraseGroup.SetActive(true);
                    retypeGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    passPhraseField.Select();
                    break;
                case States.Login:
                    header.SetActive(false);
                    titleText.gameObject.SetActive(false);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_GAME_START"));
                    loginGroup.SetActive(true);
                    accountGroup.SetActive(true);
                    findPassphraseButton.gameObject.SetActive(true);
                    loginField.Select();
                    accountAddressText.gameObject.SetActive(true);
                    bg.SetActive(true);
                    break;
                case States.FindPassphrase:
                    titleText.gameObject.SetActive(false);
                    findPassphraseTitle.gameObject.SetActive(true);
                    findPassphraseGroup.SetActive(true);
                    backToLoginButton.gameObject.SetActive(true);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_OK"));
                    findPassphraseField.Select();
                    break;
                case States.Failed:
                    var upper = _prevState.ToString().ToUpper();
                    var format = L10nManager.Localize($"UI_LOGIN_{upper}_FAIL");
                    titleText.text = string.Format(format, _prevState);
                    contentText.gameObject.SetActive(true);
                    var contentFormat = L10nManager.Localize($"UI_LOGIN_{upper}_CONTENT");
                    contentText.text = string.Format(contentFormat);
                    submitButton.SetSubmitText(L10nManager.Localize("UI_OK"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(states), states, null);
            }
            UpdateSubmitButton();
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
            return result.Score >= 2;
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
                _privateKey = CheckPrivateKey(KeyStore, loginField.text);
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
                loginField.text = string.Empty;
            }

        }

        public void Submit()
        {
            if (!submitButton.Interactable)
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
                        findPassphraseField.text = null;
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
            Debug.Log($"[LoginPopup]Show({path}, {privateKeyString}) invoked");
            KeyStore = path is null ? Web3KeyStore.DefaultKeyStore : new Web3KeyStore(path);
            _privateKeyString = privateKeyString;
            //Auto login for miner, seed, launcher
            if (!string.IsNullOrEmpty(_privateKeyString) || Application.isBatchMode)
            {
                CreatePrivateKey();
                Login = true;
                Close();
            }
            else
            {
                var state = KeyStore.ListIds().Any() ? States.Login : States.Show;
                SetState(state);
                Login = false;

                if (state == States.Login)
                {
                    // 키 고르는 게 따로 없으니 갖고 있는 키 중에서 아무거나 보여줘야 함...
                    // FIXME: 역시 키 고르는 단계가 있어야 할 것 같음
                    SetImage(KeyStore.List().First().Item2.Address);
                }

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
                base.Show();
                if (blur)
                {
                    blur.Show();
                }
            }
        }

        private void CreatePrivateKey()
        {
            PrivateKey privateKey = null;

            if (string.IsNullOrEmpty(_privateKeyString))
            {
                privateKey = CheckPrivateKey(KeyStore, passPhraseField.text);
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

        private static PrivateKey CheckPrivateKey(IKeyStore keyStore, string passphrase)
        {
            // 현재는 시스템에 키가 딱 하나만 있을 거라고 가정하고 있음.
            // UI에서도 여러 키 중 하나를 고르는 게 없기 때문에, 만약 여러 키가 있으면 입력 받은 패스프레이즈를
            // 가진 모든 키에 대해 시도함. 따라서 둘 이상의 다른 키를 같은 패스프레이즈로 잠궈둔 경우, 그 중에서
            // 뭐가 선택될 지는 알 수 없음. 대부분의 사람들이 패스프레이즈로 같은 단어만 거듭 활용하는 경향이 있기 때문에
            // 그런 케이스에서 이용자에게는 버그처럼 여겨지는 동작일지도.
            // FIXME: 따라서 UI에서 키 여러 개 중 뭘 쓸지 선택하는 걸 두는 게 좋을 듯.
            PrivateKey privateKey = null;
            foreach (var pair in keyStore.List())
            {
                pair.Deconstruct(out Guid keyId, out ProtectedPrivateKey ppk);
                try
                {
                    privateKey = ppk.Unprotect(passphrase: passphrase);
                }
                catch (IncorrectPassphraseException)
                {
                    Debug.LogWarningFormat(
                        "The key {0} is protected with a passphrase; failed to load: {1}",
                        ppk.Address,
                        keyId
                    );
                }

                Debug.LogFormat("The key {0} was successfully loaded using passphrase: {1}", ppk.Address, keyId);
                break;
            }

            return privateKey;
        }

        public PrivateKey GetPrivateKey()
        {
            return _privateKey;
        }

        private void UpdateSubmitButton()
        {
            switch (State.Value)
            {
                case States.ResetPassphrase:
                case States.CreatePassword:
                    submitButton.SetSubmittable(!(string.IsNullOrEmpty(passPhraseField.text) ||
                                                  string.IsNullOrEmpty(retypeField.text)));
                    break;

                case States.Login:
                    submitButton.SetSubmittable(!string.IsNullOrEmpty(loginField.text));
                    break;

                case States.FindPassphrase:
                    submitButton.SetSubmittable(!string.IsNullOrEmpty(findPassphraseField.text));
                    break;
                case States.CreateAccount:
                case States.Show:
                    submitButton.SetSubmittable(true);
                    break;
                case States.Failed:
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                switch (State.Value)
                {
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

        private bool CheckPrivateKeyHex()
        {
            var hex = findPassphraseField.text;
            try
            {
                var pk = new PrivateKey(ByteUtil.ParseHex(hex));
                Address address = pk.ToAddress();
                return KeyStore.List().Any(pair => pair.Item2.Address == address);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ResetPassphrase()
        {
            // 이름은 reset이라곤 하지만, 그냥 raw private key 가져오는 기능임.
            // FIXME: 전부터 이름 바꿔야 한다는 얘기가 줄곧 나왔음... ("reset passphrase"가 아니라 "import private key"로)
            var hex = findPassphraseField.text;
            var pk = new PrivateKey(ByteUtil.ParseHex(hex));

            // 가져온 비밀키를 키스토어에 넣기 전에, 혹시 같은 주소에 대한 키를 지운다.  (아무튼 기능명이 "reset"이라...)
            // 참고로 본 함수 호출되기 전에 CheckPassphrase()에서 먼저 같은 키의 비밀키가 있는지 확인한다. "찾기"가 아니라 "추가"니까, 없으면 오류가 먼저 나게 되어 있음.
            Address address = pk.ToAddress();
            Guid[] keyIdsToRemove = KeyStore.List().
                Where(pair => pair.Item2.Address.Equals(address))
                .Select(pair => pair.Item1).ToArray();
            foreach (Guid keyIdToRemove in keyIdsToRemove)
            {
                try
                {
                    KeyStore.Remove(keyIdToRemove);
                }
                catch (NoKeyException e)
                {
                    Debug.LogWarning(e);
                }
            }

            // 새로 가져온 비밀키 추가
            CreateProtectedPrivateKey(pk);
        }

        private void CreateProtectedPrivateKey(PrivateKey privateKey)
        {
            var ppk = ProtectedPrivateKey.Protect(privateKey, passPhraseField.text);
            KeyStore.Add(ppk);
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

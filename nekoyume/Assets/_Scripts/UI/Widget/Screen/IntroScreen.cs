#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet.Common;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.Game.OAuth;
using Nekoyume.L10n;
using Nekoyume.Planet;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using UniRx;

    public class IntroScreen : ScreenWidget
    {
        public enum SocialType
        {
            Google,
            Apple,
        }

        [Serializable]
        public struct AgentInfo
        {
            private const string AccountTextFormat =
                "<color=#B38271>Lv. {0}</color> {1} <color=#A68F7E>#{2}</color>";

            [NonSerialized]
            public bool isAppliedL10n;

            public TextMeshProUGUI title;
            public GameObject noAccount;
            public TextMeshProUGUI noAccountText;
            public Button noAccountCreateButton;
            public TextMeshProUGUI noAccountCreateButtonText;
            public GameObject account;
            public TextMeshProUGUI[] accountTexts;
            public Button accountImportKeyButton;
            public TextMeshProUGUI accountImportKeyButtonText;

            public PlanetId? PlanetId { get; private set; }

            public void ApplyL10n()
            {
                if (isAppliedL10n || !L10nManager.IsInitialized)
                {
                    return;
                }

                isAppliedL10n = true;
                noAccountText.text = L10nManager.Localize("SDESC_NO_ACCOUNT");
                noAccountCreateButtonText.text = L10nManager.Localize("BTN_CREATE_A_NEW_CHARACTER");
                accountImportKeyButtonText.text = L10nManager.Localize("BTN_IMPORT_KEY");
            }

            public void Set(
                PlanetContext planetContext,
                PlanetAccountInfo planetAccountInfo,
                bool needToImportKey)
            {
                ApplyL10n();

                if (planetContext?.PlanetRegistry is null ||
                    planetAccountInfo is null)
                {
                    Debug.LogError("[IntroScreen] AgentInfo.Set()... planetContext?PlanetRegistry" +
                                   " or planetAccountInfo is null");
                    return;
                }

                PlanetId = planetAccountInfo.PlanetId;
                if (planetContext.PlanetRegistry.TryGetPlanetInfoById(PlanetId.Value, out var planetInfo))
                {
                    var textInfo = CultureInfo.InvariantCulture.TextInfo;
                    title.text = textInfo.ToTitleCase(planetInfo.Name);
                }
                else
                {
                    Debug.LogError("[IntroScreen] AgentInfo.Set()... cannot find planetInfo" +
                                   $" by planetId: {PlanetId}");
                    title.text = PlanetId.ToString();
                }

                if (planetAccountInfo.AgentAddress is null)
                {
                    noAccount.SetActive(true);
                    account.SetActive(false);

                    // FIXME: Subscription does not disposed.
                    noAccountCreateButton.OnClickAsObservable()
                        .First()
                        .Subscribe(_ =>
                        {
                            // FIXME: Handle planetContext.Error.
                            PlanetSelector.SelectPlanetById(planetContext, planetInfo.ID);
                            PlanetSelector.SelectPlanetAccountInfo(
                                planetContext,
                                planetAccountInfo.PlanetId);
                        });
                }
                else
                {
                    var avatars = planetAccountInfo.AvatarGraphTypes.ToArray();
                    if (avatars.Length == 0)
                    {
                        for (var i = 0; i < accountTexts.Length; ++i)
                        {
                            var text = accountTexts[i];
                            if (i == 0)
                            {
                                text.text = L10nManager.IsInitialized
                                    ? L10nManager.Localize("SDESC_NO_CHARACTER")
                                    : "No character";
                                text.gameObject.SetActive(true);
                            }
                            else
                            {
                                text.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < accountTexts.Length; ++i)
                        {
                            var text = accountTexts[i];
                            if (avatars.Length > i)
                            {
                                var avatar = avatars[i];
                                text.text = string.Format(
                                    format: AccountTextFormat,
                                    avatar.Level,
                                    avatar.Name,
                                    avatar.Address[..4]);
                                text.gameObject.SetActive(true);
                            }
                            else
                            {
                                text.gameObject.SetActive(false);
                            }
                        }
                    }

                    accountImportKeyButtonText.text = needToImportKey
                        ? L10nManager.Localize("BTN_IMPORT_KEY")
                        : L10nManager.Localize("BTN_SELECT");
                    noAccount.SetActive(false);
                    account.SetActive(true);

                    // FIXME: Subscription does not disposed.
                    accountImportKeyButton.OnClickAsObservable()
                        .First()
                        .Subscribe(_ =>
                        {
                            // FIXME: Handle planetContext.Error.
                            PlanetSelector.SelectPlanetById(planetContext, planetInfo.ID);
                            PlanetSelector.SelectPlanetAccountInfo(
                                planetContext,
                                planetAccountInfo.PlanetId);
                        });
                }
            }
        }

        [SerializeField] private GameObject pcContainer;

        [Header("Mobile")]

        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private RawImage videoImage;

        [SerializeField] private GameObject logoAreaGO;
        [SerializeField] private GameObject touchScreenButtonGO;
        [SerializeField] private Button touchScreenButton;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button signinButton;
        [SerializeField] private Button guestButton;

        [SerializeField] private TextMeshProUGUI yourPlanetText;
        [SerializeField] private Button yourPlanetButton;
        [SerializeField] private TextMeshProUGUI yourPlanetButtonText;
        [SerializeField] private TextMeshProUGUI planetAccountInfoText;

        [SerializeField] private GameObject startButtonGO;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject socialButtonsGO;
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button appleSignInButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;
        [SerializeField] private CodeReaderView codeReaderView;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button videoSkipButton;

        [SerializeField] private GameObject selectPlanetPopup;
        [SerializeField] private TextMeshProUGUI selectPlanetPopupTitleText;
        [SerializeField] private ConditionalButton heimdallButton;
        [SerializeField] private ConditionalButton odinButton;

        [SerializeField] private GameObject planetAccountInfosPopup;
        [SerializeField] private TextMeshProUGUI planetAccountInfosTitleText;
        [SerializeField] private TextMeshProUGUI planetAccountInfosDescriptionText;
        [SerializeField] private AgentInfo planetAccountInfoLeft;
        [SerializeField] private AgentInfo planetAccountInfoRight;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;
        private PlanetContext _planetContext;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";

        public Subject<IntroScreen> OnClickTabToStart { get; } = new();
        public Subject<IntroScreen> OnClickStart { get; } = new();
        public Subject<(SocialType socialType, string email, string idToken)> OnSocialSignedIn { get; } = new();

        protected override void Awake()
        {
            base.Awake();

            touchScreenButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click touch screen button.");
                touchScreenButtonGO.SetActive(false);
                startButtonContainer.SetActive(true);
                OnClickTabToStart.OnNext(this);
            });
            startButton.onClick.AddListener(() =>
            {
                OnClickStart.OnNext(this);
                Analyzer.Instance.Track("Unity/Intro/StartButton/Click");
            });
            googleSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click google sign in button.");
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Click");
                if (!Game.Game.instance.TryGetComponent<GoogleSigninBehaviour>(out var google))
                {
                    google = Game.Game.instance.gameObject.AddComponent<GoogleSigninBehaviour>();
                }

                Debug.Log($"[IntroScreen] google.State.Value: {google.State.Value}");
                switch (google.State.Value)
                {
                    case GoogleSigninBehaviour.SignInState.Signed:
                        Debug.Log("[IntroScreen] Already signed in google. Anyway, invoke OnGoogleSignedIn.");
                        OnSocialSignedIn.OnNext((SocialType.Google, google.Email, google.IdToken));
                        return;
                    case GoogleSigninBehaviour.SignInState.Waiting:
                        Debug.Log("[IntroScreen] Already waiting for google sign in.");
                        return;
                    case GoogleSigninBehaviour.SignInState.Undefined:
                    case GoogleSigninBehaviour.SignInState.Canceled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Find<DimmedLoadingScreen>().Show(DimmedLoadingScreen.ContentType.WaitingForSocialAuthenticating);
                google.OnSignIn();
                startButtonContainer.SetActive(false);
                googleSignInButton.gameObject.SetActive(false);
                google.State
                    .SkipLatestValueOnSubscribe()
                    .First()
                    .Subscribe(state =>
                    {
                        switch (state)
                        {
                            case GoogleSigninBehaviour.SignInState.Undefined:
                            case GoogleSigninBehaviour.SignInState.Waiting:
                                return;
                            case GoogleSigninBehaviour.SignInState.Canceled:
                                startButtonContainer.SetActive(true);
                                googleSignInButton.gameObject.SetActive(true);
                                Find<DimmedLoadingScreen>().Close();
                                break;
                            case GoogleSigninBehaviour.SignInState.Signed:
                                OnSocialSignedIn.OnNext((SocialType.Google, google.Email, google.IdToken));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(state), state, null);
                        }
                    });
            });
            appleSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click apple sign in button.");
                Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Click");
                if (!Game.Game.instance.TryGetComponent<AppleSigninBehaviour>(out var apple))
                {
                    apple = Game.Game.instance.gameObject.AddComponent<AppleSigninBehaviour>();
                    apple.Initialize();
                }

                Debug.Log($"[IntroScreen] apple.State.Value: {apple.State.Value}");
                switch (apple.State.Value)
                {
                    case AppleSigninBehaviour.SignInState.Signed:
                        Debug.Log("[IntroScreen] Already signed in apple. Anyway, invoke OnAppleSignedIn.");
                        OnSocialSignedIn.OnNext((SocialType.Apple, apple.Email, apple.IdToken));
                        return;
                    case AppleSigninBehaviour.SignInState.Waiting:
                        Debug.Log("[IntroScreen] Already waiting for apple sign in.");
                        return;
                    case AppleSigninBehaviour.SignInState.Undefined:
                    case AppleSigninBehaviour.SignInState.Canceled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Find<DimmedLoadingScreen>().Show(DimmedLoadingScreen.ContentType.WaitingForSocialAuthenticating);
                apple.OnSignIn();
                startButtonContainer.SetActive(false);
                appleSignInButton.gameObject.SetActive(false);
                apple.State
                    .SkipLatestValueOnSubscribe()
                    .First()
                    .Subscribe(state =>
                    {
                        switch (state)
                        {
                            case AppleSigninBehaviour.SignInState.Undefined:
                            case AppleSigninBehaviour.SignInState.Waiting:
                                return;
                            case AppleSigninBehaviour.SignInState.Canceled:
                                startButtonContainer.SetActive(true);
                                appleSignInButton.gameObject.SetActive(true);
                                Find<DimmedLoadingScreen>().Close();
                                break;
                            case AppleSigninBehaviour.SignInState.Signed:
                                OnSocialSignedIn.OnNext((SocialType.Apple, apple.Email, apple.IdToken));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(state), state, null);
                        }
                    });
            });
            signinButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/Intro/SigninButton/Click");
                qrCodeGuideBackground.Show();
                qrCodeGuideContainer.SetActive(true);
                foreach (var image in qrCodeGuideImages)
                {
                    image.SetActive(false);
                }

                _guideIndex = 0;
                ShowQrCodeGuide();
            });
            qrCodeGuideNextButton.onClick.AddListener(() =>
            {
                _guideIndex++;
                ShowQrCodeGuide();
            });
            yourPlanetButton.onClick.AddListener(() => selectPlanetPopup.SetActive(true));
            heimdallButton.OnClickSubject
                .Subscribe(_ => selectPlanetPopup.SetActive(false))
                .AddTo(gameObject);
            heimdallButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                _planetContext = PlanetSelector.SelectPlanetByName(_planetContext, heimdallButton.Text);
                selectPlanetPopup.SetActive(false);
            }).AddTo(gameObject);
            odinButton.OnClickSubject
                .Subscribe(_ => selectPlanetPopup.SetActive(false))
                .AddTo(gameObject);
            odinButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                _planetContext = PlanetSelector.SelectPlanetByName(_planetContext, odinButton.Text);
                selectPlanetPopup.SetActive(false);
            }).AddTo(gameObject);
            planetAccountInfoLeft.noAccountCreateButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] planetAccountInfoLeft.noAccountCreateButton.onClick invoked");
                planetAccountInfosPopup.SetActive(false);
            });
            planetAccountInfoLeft.accountImportKeyButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] planetAccountInfoLeft.accountImportKeyButton.onClick invoked");
                planetAccountInfosPopup.SetActive(false);
            });
            planetAccountInfoRight.noAccountCreateButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] planetAccountInfoRight.noAccountCreateButton.onClick invoked");
                planetAccountInfosPopup.SetActive(false);
            });
            planetAccountInfoRight.accountImportKeyButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] planetAccountInfoRight.accountImportKeyButton.onClick invoked");
                planetAccountInfosPopup.SetActive(false);
            });
            PlanetSelector.CurrentPlanetInfoSubject
                .Subscribe(tuple => ApplyCurrentPlanetInfo(tuple.planetContext))
                .AddTo(gameObject);

            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            videoSkipButton.interactable = true;
            googleSignInButton.interactable = true;
#if UNITY_IOS
            appleSignInButton.gameObject.SetActive(true);
            appleSignInButton.interactable = true;
#else
            appleSignInButton.gameObject.SetActive(false);
#endif
            GetGuestPrivateKey();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnSocialSignedIn.Dispose();
        }

        public void ApplyL10n()
        {
            yourPlanetText.text = L10nManager.Localize("UI_YOUR_PLANET");
            // startButton
            selectPlanetPopupTitleText.text = L10nManager.Localize("UI_SELECT_YOUR_PLANET");
            planetAccountInfosTitleText.text = L10nManager.Localize("WORD_NOTIFICATION");
            planetAccountInfosDescriptionText.text =
                L10nManager.Localize("STC_MULTIPLANETARY_AGENT_INFOS_POPUP_ACCOUNT_ALREADY_EXIST");
            planetAccountInfoLeft.ApplyL10n();
            planetAccountInfoRight.ApplyL10n();
        }

        public void SetData(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            _planetContext = planetContext;
            ApplyCurrentPlanetInfo(_planetContext);
        }

        public void ShowTabToStart()
        {
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
            logoAreaGO.SetActive(false);
            touchScreenButtonGO.SetActive(true);
            startButtonContainer.SetActive(false);
            qrCodeGuideContainer.SetActive(false);
        }

        public void Show(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            Analyzer.Instance.Track("Unity/Intro/Show");
            SetData(keyStorePath, privateKey, planetContext);

#if RUN_ON_MOBILE
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
            logoAreaGO.SetActive(false);
            touchScreenButtonGO.SetActive(false);
            startButtonContainer.SetActive(true);
            qrCodeGuideContainer.SetActive(false);
            ShowMobile();
#else
            pcContainer.SetActive(true);
            mobileContainer.SetActive(false);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            Find<LoginSystem>().Show(_keyStorePath, _privateKey);
#endif
        }

        public void ShowForQrCodeGuide()
        {
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
            logoAreaGO.SetActive(false);
            touchScreenButtonGO.SetActive(false);
            startButtonContainer.SetActive(false);
            qrCodeGuideContainer.SetActive(false);

            qrCodeGuideBackground.Show();
            qrCodeGuideContainer.SetActive(true);
            foreach (var image in qrCodeGuideImages)
            {
                image.SetActive(false);
            }

            _guideIndex = 0;
            ShowQrCodeGuide();
        }

        public void ShowMobile()
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            Analyzer.Instance.Track("Unity/Intro/StartButton/Show");
        }

        public void ShowPlanetAccountInfosPopup(PlanetContext planetContext, bool needToImportKey)
        {
            Debug.Log("[IntroScreen] ShowPlanetAccountInfosPopup invoked");
            if (planetContext.PlanetAccountInfos is null)
            {
                Debug.LogError("[IntroScreen] planetContext.PlanetAccountInfos is null");
            }

            planetAccountInfoLeft.Set(
                planetContext,
                planetContext.PlanetAccountInfos?.FirstOrDefault(info =>
                    info.PlanetId.Equals(PlanetId.Odin) ||
                    info.PlanetId.Equals(PlanetId.OdinInternal)),
                needToImportKey);
            planetAccountInfoRight.Set(
                planetContext,
                planetContext.PlanetAccountInfos?.FirstOrDefault(info =>
                    info.PlanetId.Equals(PlanetId.Heimdall) ||
                    info.PlanetId.Equals(PlanetId.HeimdallInternal)),
                needToImportKey);
            planetAccountInfosPopup.SetActive(true);
        }

        private void OnVideoEnd()
        {
            Analyzer.Instance.Track("Unity/Intro/Video/End");
            videoImage.gameObject.SetActive(false);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
        }

        private void ShowQrCodeGuide()
        {
            if (_guideIndex >= GuideCount)
            {
                _guideIndex = 0;
                qrCodeGuideContainer.SetActive(false);

                codeReaderView.Show(res =>
                {
                    var loginSystem = Find<LoginSystem>();
                    loginSystem.KeyStore.Add(ProtectedPrivateKey.FromJson(res.Text));
                    codeReaderView.Close();
                    startButtonContainer.SetActive(false);
                    loginSystem.Show(_keyStorePath, _privateKey);
                    Analyzer.Instance.Track("Unity/Intro/QRCodeImported");
                });
            }
            else
            {
                Analyzer.Instance.Track($"Unity/Intro/GuideDMX/{_guideIndex + 1}");
                qrCodeGuideImages[_guideIndex].SetActive(true);
                qrCodeGuideText.text = L10nManager.Localize($"INTRO_QR_CODE_GUIDE_{_guideIndex}");
            }
        }

        private async void GetGuestPrivateKey()
        {
            string pk;
            try
            {
                var request = UnityWebRequest.Get(GuestPrivateKeyUrl);
                await request.SendWebRequest();
                pk = request.downloadHandler.text.Trim();
                ByteUtil.ParseHex(pk);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get guest private key: {e}");
                return;
            }

            guestButton.gameObject.SetActive(true);
            guestButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/Intro/Guest/Click");
                startButtonContainer.SetActive(false);
                Find<LoginSystem>().Show(_keyStorePath, pk);
            });
            guestButton.interactable = true;
        }

#if RUN_ON_MOBILE
        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();

            MainCanvas.instance.RemoveWidget(this);
        }
#endif

        private void ApplyCurrentPlanetInfo(PlanetContext planetContext)
        {
            var planetRegistry = planetContext?.PlanetRegistry;
            var planetInfo = planetContext?.SelectedPlanetInfo;
            if (planetRegistry is null ||
                planetInfo is null)
            {
                yourPlanetButtonText.text = "Null";
                planetAccountInfoText.text = string.Empty;
                heimdallButton.Interactable = false;
                heimdallButton.Text = "Heimdall (Null)";
                odinButton.Interactable = false;
                odinButton.Text = "Odin (Null)";
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            yourPlanetButtonText.text = textInfo.ToTitleCase(planetInfo.Name);
            if (planetContext.SelectedPlanetAccountInfo is null)
            {
                planetAccountInfoText.text = string.Empty;
            }
            else
            {
                var avatarCount = planetContext.SelectedPlanetAccountInfo.AvatarGraphTypes.Count();
                planetAccountInfoText.text = avatarCount switch
                {
                    0 => L10nManager.Localize("SDESC_THERE_IS_NO_CHARACTER"),
                    1 => L10nManager.Localize("SDESC_THERE_IS_ONE_CHARACTER"),
                    _ => L10nManager.Localize("SDESC_THERE_ARE_0_CHARACTERS_FORMAT", avatarCount)
                };
            }

            if (planetContext.SkipSocialAndPortalLogin.HasValue &&
                planetContext.SkipSocialAndPortalLogin.Value)
            {
                startButtonGO.SetActive(true);
                socialButtonsGO.SetActive(false);
            }
            else
            {
                startButtonGO.SetActive(false);
                socialButtonsGO.SetActive(true);
            }

            if (planetRegistry.TryGetPlanetInfoById(PlanetId.Heimdall, out var heimdallInfo) ||
                planetRegistry.TryGetPlanetInfoById(PlanetId.HeimdallInternal, out heimdallInfo))
            {
                heimdallButton.Text = textInfo.ToTitleCase(heimdallInfo.Name);
            }

            if (planetRegistry.TryGetPlanetInfoById(PlanetId.Odin, out var odinInfo) ||
                planetRegistry.TryGetPlanetInfoById(PlanetId.OdinInternal, out odinInfo))
            {
                odinButton.Text = textInfo.ToTitleCase(odinInfo.Name);
            }

            if (planetInfo.ID.Equals(PlanetId.Odin) ||
                planetInfo.ID.Equals(PlanetId.OdinInternal))
            {
                heimdallButton.Interactable = false;
                odinButton.Interactable = true;
            }
            else
            {
                heimdallButton.Interactable = true;
                odinButton.Interactable = false;
            }
        }
    }
}

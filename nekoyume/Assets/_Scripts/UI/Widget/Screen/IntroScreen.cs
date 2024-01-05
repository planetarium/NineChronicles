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
using Nekoyume.Model.Mail;
using Nekoyume.Multiplanetary;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using Nekoyume.Blockchain;
    using UniRx;

    public class IntroScreen : ScreenWidget
    {
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
        [SerializeField] private Button appleSignInButton;
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button twitterSignInButton;
        [SerializeField] private Button discordSignInButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;
        [SerializeField] private CodeReaderView codeReaderView;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button videoSkipButton;

        [SerializeField] private GameObject selectPlanetPopup;
        [SerializeField] private Button selectPlanetPopupBgButton;
        [SerializeField] private TextMeshProUGUI selectPlanetPopupTitleText;
        [SerializeField] private SelectPlanetScroll selectPlanetScroll;

        [SerializeField] private GameObject planetAccountInfosPopup;
        [SerializeField] private TextMeshProUGUI planetAccountInfosTitleText;
        [SerializeField] private TextMeshProUGUI planetAccountInfosDescriptionText;
        [SerializeField] private PlanetAccountInfoScroll planetAccountInfoScroll;

        private const int GuideCount = 3;
        private const int GuideStartIndex = 1;
        private int _guideIndex = GuideStartIndex;

        private string _keyStorePath;
        private string _privateKey;
        private PlanetContext _planetContext;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";

        public Subject<IntroScreen> OnClickTabToStart { get; } = new();
        public Subject<IntroScreen> OnClickStart { get; } = new();
        public Subject<(SigninContext.SocialType socialType, string email, string idToken)> OnSocialSignedIn { get; } = new();

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
                Debug.Log("[IntroScreen] Click start button.");
                Analyzer.Instance.Track("Unity/Intro/StartButton/Click");

                var evt = new AirbridgeEvent("Intro_StartButton_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                OnClickStart.OnNext(this);
            });
            appleSignInButton.onClick.AddListener(() =>
            {
#if !UNITY_IOS
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("SDESC_THIS_PLATFORM_IS_NOT_SUPPORTED"),
                    NotificationCell.NotificationType.Information);
                return;
#endif

                Debug.Log("[IntroScreen] Click apple sign in button.");
                Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Click");

                var evt = new AirbridgeEvent("Intro_AppleSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                ProcessAppleSigning();
            });
            googleSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click google sign in button.");
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Click");

                var evt = new AirbridgeEvent("Intro_GoogleSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                ProcessGoogleSigning();
            });
            twitterSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click twitter sign in button.");

                Analyzer.Instance.Track("Unity/Intro/TwitterSignIn/Click");

                var evt = new AirbridgeEvent("Intro_TwitterSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                ShowPortalConnectGuidePopup(SigninContext.SocialType.Twitter);
            });
            discordSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click discord sign in button.");

                Analyzer.Instance.Track("Unity/Intro/DiscordSignIn/Click");

                var evt = new AirbridgeEvent("Intro_DiscordSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                ShowPortalConnectGuidePopup(SigninContext.SocialType.Discord);
            });
            signinButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/Intro/SigninButton/Click");

                var evt = new AirbridgeEvent("Intro_SigninButton_Click");
                AirbridgeUnity.TrackEvent(evt);

                qrCodeGuideBackground.Show();
                qrCodeGuideContainer.SetActive(true);
                foreach (var image in qrCodeGuideImages)
                {
                    image.SetActive(false);
                }

                _guideIndex = GuideStartIndex;
                ShowQrCodeGuide();
            });
            qrCodeGuideNextButton.onClick.AddListener(() =>
            {
                _guideIndex++;
                ShowQrCodeGuide();
            });
            yourPlanetButton.onClick.AddListener(() => selectPlanetPopup.SetActive(true));
            selectPlanetPopupBgButton.onClick.AddListener(() => selectPlanetPopup.SetActive(false));
            selectPlanetScroll.OnChangeSelectedPlanetSubject
                .Subscribe(tuple =>
                {
                    // NOTE: Do not handle the PlanetContext.Error now.
                    _planetContext = PlanetSelector.SelectPlanetById(
                        _planetContext,
                        tuple.selectedPlanetId);
                    selectPlanetPopup.SetActive(false);
                })
                .AddTo(gameObject);
            selectPlanetScroll.OnClickSelectedPlanetSubject
                .Subscribe(_ => selectPlanetPopup.SetActive(false))
                .AddTo(gameObject);
            planetAccountInfoScroll.OnSelectedPlanetSubject.Subscribe(tuple =>
            {
                // NOTE: Do not handle the PlanetContext.Error now.
                _planetContext = PlanetSelector.SelectPlanetById(
                    _planetContext,
                    tuple.selectedPlanetId);
                _planetContext = PlanetSelector.SelectPlanetAccountInfo(
                    _planetContext,
                    tuple.selectedPlanetId);
                planetAccountInfosPopup.SetActive(false);
            }).AddTo(gameObject);
            PlanetSelector.SelectedPlanetInfoSubject
                .Subscribe(tuple => ApplySelectedPlanetInfo(tuple.planetContext))
                .AddTo(gameObject);
            PlanetSelector.SelectedPlanetAccountInfoSubject
                .Subscribe(tuple => ApplySelectedPlanetAccountInfo(tuple.planetContext))
                .AddTo(gameObject);

            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            videoSkipButton.interactable = true;
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
        }

        public void SetData(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            _planetContext = planetContext;
            ApplyPlanetContext(_planetContext);

            if (SigninContext.HasLatestSignedInSocialType)
            {
                Debug.Log("[IntroScreen] SetData: SigninContext.HasLatestSignedInSocialType is true");
                startButtonGO.SetActive(true);
                socialButtonsGO.SetActive(false);
            }
            else
            {
                Debug.Log("[IntroScreen] SetData: SigninContext.HasLatestSignedInSocialType is false");
                startButtonGO.SetActive(false);
                socialButtonsGO.SetActive(true);
            }
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

            var evt = new AirbridgeEvent("Intro_Show");
            AirbridgeUnity.TrackEvent(evt);

            SetData(keyStorePath, privateKey, planetContext);

#if RUN_ON_MOBILE
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
            logoAreaGO.SetActive(false);
            touchScreenButtonGO.SetActive(false);
            startButtonContainer.SetActive(true);
            qrCodeGuideContainer.SetActive(false);
#else
            pcContainer.SetActive(true);
            mobileContainer.SetActive(false);
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

            _guideIndex = GuideStartIndex;
            ShowQrCodeGuide();
        }

        /// <summary>
        /// The only way to update the planetAccountInfoScroll state.
        /// </summary>
        public void ShowPlanetAccountInfosPopup(PlanetContext planetContext, bool needToImportKey)
        {
            Debug.Log("[IntroScreen] ShowPlanetAccountInfosPopup invoked");
            if (planetContext.PlanetAccountInfos is null)
            {
                Debug.LogError("[IntroScreen] planetContext.PlanetAccountInfos is null");
            }

            planetAccountInfoScroll.SetData(
                planetContext.PlanetRegistry,
                planetContext.PlanetAccountInfos,
                needToImportKey);
            planetAccountInfosPopup.SetActive(true);
            startButtonContainer.SetActive(false);
        }

        private void OnVideoEnd()
        {
            Analyzer.Instance.Track("Unity/Intro/Video/End");

            var evt = new AirbridgeEvent("Intro_Video_End");
            AirbridgeUnity.TrackEvent(evt);

            videoImage.gameObject.SetActive(false);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
        }

        private void ShowQrCodeGuide()
        {
            if (_guideIndex >= GuideCount)
            {
                _guideIndex = GuideStartIndex;
                qrCodeGuideContainer.SetActive(false);

                codeReaderView.Show(res =>
                {
                    var resultPpk = ProtectedPrivateKey.FromJson(res.Text);
                    var requiredAddress = resultPpk.Address;
                    var km = KeyManager.Instance;
                    if (km.Has(requiredAddress))
                    {
                        km.BackupKey(requiredAddress, keyStorePathToBackup: null);
                    }

                    km.Register(resultPpk);
                    codeReaderView.Close();
                    startButtonContainer.SetActive(false);
                    Find<LoginSystem>().Show(_keyStorePath, string.Empty);
                    Analyzer.Instance.Track("Unity/Intro/QRCodeImported");

                    var evt = new AirbridgeEvent("Intro_QRCodeImported");
                    AirbridgeUnity.TrackEvent(evt);
                });
            }
            else
            {
                Analyzer.Instance.Track($"Unity/Intro/GuideDMX/{_guideIndex + 1}");

                var evt = new AirbridgeEvent("Intro_GuideDMX");
                evt.SetValue(_guideIndex + 1);
                AirbridgeUnity.TrackEvent(evt);

                foreach (var qrCodeGuideImage in qrCodeGuideImages)
                {
                    qrCodeGuideImage.SetActive(false);
                }

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

                var evt = new AirbridgeEvent("Intro_Guest_Click");
                AirbridgeUnity.TrackEvent(evt);

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

        private void ApplyPlanetContext(PlanetContext planetContext)
        {
            Debug.Log("[IntroScreen] ApplyPlanetRegistry invoked.");
            selectPlanetScroll.SetData(
                planetContext?.PlanetRegistry,
                planetContext?.SelectedPlanetInfo?.ID);

            ApplySelectedPlanetInfo(planetContext);
            ApplySelectedPlanetAccountInfo(planetContext);
        }

        private void ApplySelectedPlanetInfo(PlanetContext planetContext)
        {
            Debug.Log("[IntroScreen] ApplySelectedPlanetInfo invoked.");
            var planetInfo = planetContext?.SelectedPlanetInfo;
            if (planetInfo is null)
            {
                Debug.Log("[IntroScreen] ApplySelectedPlanetInfo... planetInfo is null");
                yourPlanetButtonText.text = "Null";
                planetAccountInfoText.text = string.Empty;
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            yourPlanetButtonText.text = textInfo.ToTitleCase(planetInfo.Name);
        }

        private void ApplySelectedPlanetAccountInfo(PlanetContext planetContext)
        {
            Debug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo invoked.");
            var planetAccountInfo = planetContext?.SelectedPlanetAccountInfo;
            if (planetAccountInfo?.AgentAddress is null)
            {
                Debug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo... planetAccountInfo?.AgentAddress is null.");
                planetAccountInfoText.text = SigninContext.HasLatestSignedInSocialType
                    ? L10nManager.Localize("SDESC_THERE_IS_NO_ACCOUNT")
                    : string.Empty;

                return;
            }

            if (!(planetAccountInfo.IsAgentPledged.HasValue &&
                  planetAccountInfo.IsAgentPledged.Value))
            {
                Debug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo... planetAccountInfo.IsAgentPledged is false.");
                planetAccountInfoText.text = L10nManager.Localize("SDESC_THERE_IS_NO_CHARACTER");
                return;
            }

            var avatarCount = planetAccountInfo.AvatarGraphTypes.Count();
            planetAccountInfoText.text = avatarCount switch
            {
                0 => L10nManager.Localize("SDESC_THERE_IS_NO_CHARACTER"),
                1 => L10nManager.Localize("SDESC_THERE_IS_ONE_CHARACTER"),
                _ => L10nManager.Localize("SDESC_THERE_ARE_0_CHARACTERS_FORMAT", avatarCount)
            };
        }

        private void ProcessGoogleSigning()
        {
            if (!Game.Game.instance.TryGetComponent<GoogleSigninBehaviour>(out var google))
            {
                google = Game.Game.instance.gameObject.AddComponent<GoogleSigninBehaviour>();
            }

            Debug.Log($"[IntroScreen] google.State.Value: {google.State.Value}");
            switch (google.State.Value)
            {
                case GoogleSigninBehaviour.SignInState.Signed:
                    Debug.Log("[IntroScreen] Already signed in google. Anyway, invoke OnGoogleSignedIn.");
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Google);
                    OnSocialSignedIn.OnNext((SigninContext.SocialType.Google, google.Email, google.IdToken));
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
                            Find<DimmedLoadingScreen>().Close();
                            break;
                        case GoogleSigninBehaviour.SignInState.Signed:
                            SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Google);
                            OnSocialSignedIn.OnNext((SigninContext.SocialType.Google, google.Email, google.IdToken));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                    }
                });
        }

        private void ProcessAppleSigning()
        {
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
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Apple);
                    OnSocialSignedIn.OnNext((SigninContext.SocialType.Apple, apple.Email, apple.IdToken));
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
                            Find<DimmedLoadingScreen>().Close();
                            break;
                        case AppleSigninBehaviour.SignInState.Signed:
                            SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Apple);
                            OnSocialSignedIn.OnNext((SigninContext.SocialType.Apple, apple.Email, apple.IdToken));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(state), state, null);
                    }
                });
        }

        private void ShowPortalConnectGuidePopup(SigninContext.SocialType socialType)
        {
            if (!TryFind<TitleOneButtonSystem>(out var popup))
            {
                popup = Create<TitleOneButtonSystem>();
            }

            popup.SubmitCallback = () =>
            {
                popup.Close();
                Application.OpenURL("http://nine-chronicles.com/connect-guide");
            };
            popup.Show(
                L10nManager.Localize("UI_INFORMATION_CHARACTER_SELECT"),
                L10nManager.Localize(
                    "STS_YOU_CAN_CONNECT_0_TO_APPLE_OR_GOOGLE_ON_PORTAL_FORMAT",
                    socialType.ToString()),
                L10nManager.Localize("BTN_OPEN_A_BROWSER"),
                localize: false);
        }
    }
}

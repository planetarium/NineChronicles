#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.Game.OAuth;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Multiplanetary;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using ZXing;

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

        [Header("Mobile/Logo")]
        [SerializeField] private GameObject logoAreaGO;
        [SerializeField] private GameObject touchScreenButtonGO;
        [SerializeField] private Button touchScreenButton;
        [Space]
        [SerializeField] private Sprite logoMSprite;
        [SerializeField] private Sprite logoKSprite;
        [SerializeField] private Image[] logoImages;

        [Header("Mobile/StartButton")]
        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button signinButton;
        [SerializeField] private Button guestButton;
        [SerializeField] private Button backupButton;
        [SerializeField] private Button keyImportButton;

        [Header("Mobile/YourPlanet")]
        [SerializeField] private Button yourPlanetButton;
        [SerializeField] private TextMeshProUGUI yourPlanetButtonText;
        [SerializeField] private TextMeshProUGUI planetAccountInfoText;

        [Header("Mobile/SocialButtons")]
        [SerializeField] private GameObject startButtonGO;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject socialButtonsGO;
        [SerializeField] private Button appleSignInButton;
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button twitterSignInButton;
        [SerializeField] private Button discordSignInButton;

        [Header("Mobile/QRCodeGuide")]
        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;
        [SerializeField] private CodeReaderView codeReaderView;

        [Header("Mobile/Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button videoSkipButton;

        [Header("Mobile/SelectPlanetPopup")]
        [SerializeField] private GameObject selectPlanetPopup;
        [SerializeField] private Button selectPlanetPopupBgButton;
        [SerializeField] private SelectPlanetScroll selectPlanetScroll;

        [Header("Mobile/PlanetAccountInfosPopup")]
        [SerializeField] private GameObject planetAccountInfosPopup;
        [SerializeField] private PlanetAccountInfoScroll planetAccountInfoScroll;

        [Header("Mobile/KeyImportPopup")]
        [SerializeField] private GameObject keyImportPopup;
        [SerializeField] private Button keyImportCloseButton;
        [SerializeField] private Button keyImportWithCameraButton;
        [SerializeField] private Button keyImportWithGalleryButton;

        private string _keyStorePath;
        private string _privateKey;
        private PlanetContext _planetContext;
        private bool _isSetGuestPrivateKey = false;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";

        public Subject<IntroScreen> OnClickTabToStart { get; } = new();
        public Subject<IntroScreen> OnClickStart { get; } = new();
        public Subject<(SigninContext.SocialType socialType, string email, string idToken)> OnSocialSignedIn { get; } = new();

        protected override void Awake()
        {
            base.Awake();

            twitterSignInButton.gameObject.SetActive(!Game.LiveAsset.GameConfig.IsKoreanBuild);
            discordSignInButton.gameObject.SetActive(!Game.LiveAsset.GameConfig.IsKoreanBuild);
            foreach (var logoImage in logoImages)
            {
                logoImage.sprite = Game.LiveAsset.GameConfig.IsKoreanBuild
                    ? logoKSprite
                    : logoMSprite;
            }

            touchScreenButton.onClick.AddListener(() =>
            {
                NcDebug.Log("[IntroScreen] Click touch screen button.");
                touchScreenButtonGO.SetActive(false);
                startButtonContainer.SetActive(true);
                OnClickTabToStart.OnNext(this);
            });
            startButton.onClick.AddListener(() =>
            {
                NcDebug.Log("[IntroScreen] Click start button.");
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

                NcDebug.Log("[IntroScreen] Click apple sign in button.");
                Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Click");

                var evt = new AirbridgeEvent("Intro_AppleSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                ProcessAppleSigning();
            });
            googleSignInButton.onClick.AddListener(() =>
            {
                NcDebug.Log("[IntroScreen] Click google sign in button.");
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Click");

                var evt = new AirbridgeEvent("Intro_GoogleSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                ProcessGoogleSigning();
            });
            twitterSignInButton.onClick.AddListener(() =>
            {
                NcDebug.Log("[IntroScreen] Click twitter sign in button.");

                Analyzer.Instance.Track("Unity/Intro/TwitterSignIn/Click");

                var evt = new AirbridgeEvent("Intro_TwitterSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                ShowPortalConnectGuidePopup(SigninContext.SocialType.Twitter);
            });
            discordSignInButton.onClick.AddListener(() =>
            {
                NcDebug.Log("[IntroScreen] Click discord sign in button.");

                Analyzer.Instance.Track("Unity/Intro/DiscordSignIn/Click");

                var evt = new AirbridgeEvent("Intro_DiscordSignIn_Click");
                AirbridgeUnity.TrackEvent(evt);

                ShowPortalConnectGuidePopup(SigninContext.SocialType.Discord);
            });
            // NOTE: this button is not used now.
            signinButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/Intro/SigninButton/Click");

                var evt = new AirbridgeEvent("Intro_SigninButton_Click");
                AirbridgeUnity.TrackEvent(evt);

                ShowQrCodeGuide(result =>
                {
                    var pk = ImportPrivateKeyFromJson(result.Text);
                    startButtonContainer.SetActive(false);
                    Find<LoginSystem>().Show(privateKeyString: pk?.ToHexWithZeroPaddings() ?? string.Empty);
                });
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

            var ppkExist = IsProtectedPrivateKeyExist();
            backupButton.gameObject.SetActive(ppkExist);
            backupButton.onClick.AddListener(() =>
            {
                var keys = KeyManager.Instance.GetList().ToList();
                if (keys.Any())
                {
                    var firstKey = keys.First().Item2;
                    if (KeyManager.Instance.GetCachedPassphrase(firstKey.Address)
                        .Equals(string.Empty))
                    {
                        Find<LoginSystem>().ShowResetPassword();
                    }
                    else
                    {
                        new NativeShare().AddFile(Util.GetQrCodePngFromKeystore(), "shareQRImg.png")
                            .SetSubject(L10nManager.Localize("UI_SHARE_QR_TITLE"))
                            .SetText(L10nManager.Localize("UI_SHARE_QR_CONTENT"))
                            .Share();
                    }
                }
            });

            var trySigninWithKeyImport = false;
            keyImportButton.gameObject.SetActive(!ppkExist);
            keyImportButton.onClick.AddListener(() =>
            {
                keyImportPopup.SetActive(true);
                keyImportCloseButton.gameObject.SetActive(true);
                trySigninWithKeyImport = true;
            });
            keyImportCloseButton.onClick.AddListener(() =>
            {
                keyImportPopup.SetActive(false);
                keyImportCloseButton.gameObject.SetActive(false);
                trySigninWithKeyImport = false;
            });

            keyImportWithCameraButton.onClick.AddListener(() =>
            {
                keyImportPopup.SetActive(false);
                ShowQrCodeGuide(result =>
                {
                    var pk = ImportPrivateKeyFromJson(result.Text);
                    SigninContext.SetHasSignedWithKeyImport(trySigninWithKeyImport);
                    startButtonContainer.SetActive(false);
                    Find<LoginSystem>().Show(privateKeyString: pk?.ToHexWithZeroPaddings() ?? string.Empty);
                });
            });
            keyImportWithGalleryButton.onClick.AddListener(() =>
            {
                codeReaderView.ScanQrCodeFromGallery(result =>
                {
                    if (result == null)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("ERROR_IMPORTKEY_LOADIMAGE"),
                            NotificationCell.NotificationType.Alert);
                        return;
                    }

                    var pk = ImportPrivateKeyFromJson(result.Text);
                    SigninContext.SetHasSignedWithKeyImport(trySigninWithKeyImport);
                    keyImportPopup.SetActive(false);
                    startButtonContainer.SetActive(false);
                    Find<LoginSystem>().Show(privateKeyString: pk?.ToHexWithZeroPaddings() ?? string.Empty);
                });
            });
        }

        private static PrivateKey ImportPrivateKeyFromJson(string json)
        {
            var resultPpk = ProtectedPrivateKey.FromJson(json);
            var requiredAddress = resultPpk.Address;
            var km = KeyManager.Instance;
            if (km.Has(requiredAddress))
            {
                km.BackupKey(requiredAddress, keyStorePathToBackup: null);
            }

            km.Register(resultPpk);
            PrivateKey pk = null;
            try
            {
                pk = resultPpk.Unprotect(string.Empty);
            }
            catch
            {
                // ignored
            }

            Analyzer.Instance.Track("Unity/Intro/QRCodeImported");

            var evt = new AirbridgeEvent("Intro_QRCodeImported");
            AirbridgeUnity.TrackEvent(evt);

            return pk;
        }

        private static bool IsProtectedPrivateKeyExist()
        {
            Web3KeyStore keyStore;
            if (Platform.IsMobilePlatform())
            {
                var dataPath = Platform.GetPersistentDataPath("keystore");
                keyStore = new Web3KeyStore(dataPath);
            }
            else
            {
                keyStore = Web3KeyStore.DefaultKeyStore;
            }

            return keyStore.ListIds().Any();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnSocialSignedIn.Dispose();
        }

        public void SetData(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            _planetContext = planetContext;
            ApplyPlanetContext(_planetContext);

            if (SigninContext.HasLatestSignedInSocialType || SigninContext.HasSignedWithKeyImport)
            {
                NcDebug.Log("[IntroScreen] SetData: SigninContext.HasLatestSignedInSocialType is true");
                startButtonGO.SetActive(true);
                socialButtonsGO.SetActive(false);
            }
            else
            {
                NcDebug.Log("[IntroScreen] SetData: SigninContext.HasLatestSignedInSocialType is false");
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
            Find<LoginSystem>().Show(privateKeyString: _privateKey);
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

            keyImportPopup.SetActive(true);
        }

        /// <summary>
        /// The only way to update the planetAccountInfoScroll state.
        /// </summary>
        public void ShowPlanetAccountInfosPopup(PlanetContext planetContext, bool needToImportKey)
        {
            NcDebug.Log("[IntroScreen] ShowPlanetAccountInfosPopup invoked");
            if (planetContext.PlanetAccountInfos is null)
            {
                NcDebug.LogError("[IntroScreen] planetContext.PlanetAccountInfos is null");
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

        private void ShowQrCodeGuide(Action<Result> onSuccess = null)
        {
            const int guideStartIndex = 1; // pass 0
            const int guideCount = 3;

            qrCodeGuideBackground.Show();
            qrCodeGuideContainer.SetActive(true);

            var guideIndex = guideStartIndex;
            GuideImage(guideIndex);

            qrCodeGuideNextButton.onClick.RemoveAllListeners();
            qrCodeGuideNextButton.onClick.AddListener(() =>
            {
                guideIndex++;
                if (guideIndex >= guideCount)
                {
                    qrCodeGuideContainer.SetActive(false);
                    codeReaderView.Show(result =>
                    {
                        codeReaderView.Close();
                        onSuccess?.Invoke(result);
                    });
                }
                else
                {
                    Analyzer.Instance.Track($"Unity/Intro/GuideDMX/{guideIndex + 1}");

                    var evt = new AirbridgeEvent("Intro_GuideDMX");
                    evt.SetValue(guideIndex + 1);
                    AirbridgeUnity.TrackEvent(evt);

                    GuideImage(guideIndex);
                }
            });
            return;

            void GuideImage(int index)
            {
                foreach (var qrCodeGuideImage in qrCodeGuideImages)
                {
                    qrCodeGuideImage.SetActive(false);
                }

                qrCodeGuideImages[index].SetActive(true);
                qrCodeGuideText.text = L10nManager.Localize($"INTRO_QR_CODE_GUIDE_{index}");
            }
        }

        public async void GetGuestPrivateKey()
        {
            string pk;
            try
            {
                await UniTask.SwitchToMainThread();
                var request = UnityWebRequest.Get(GuestPrivateKeyUrl);
                await request.SendWebRequest();
                pk = request.downloadHandler.text.Trim();
                ByteUtil.ParseHex(pk);
                NcDebug.LogWarning($"[IntroScreen] [GetGuestPrivateKey] GuestPrivateKeyUrl success");
            }
            catch (Exception e)
            {
                NcDebug.LogWarning($"[IntroScreen] [GetGuestPrivateKey] Failed to get guest private key: {e}");
                return;
            }

            if(Game.Game.instance.CommandLineOptions == null || !Game.Game.instance.CommandLineOptions.EnableGuestLogin)
            {
                NcDebug.LogError($"[IntroScreen] [GetGuestPrivateKey] Failed find Commandlineoptions");
                return;
            }

            if (_isSetGuestPrivateKey)
            {
                NcDebug.LogWarning($"[IntroScreen] [GetGuestPrivateKey] Already set guest private key");
                return;
            }

            guestButton.gameObject.SetActive(true);
            guestButton.onClick.AddListener(() =>
            {
                Analyzer.Instance.Track("Unity/Intro/Guest/Click");

                var evt = new AirbridgeEvent("Intro_Guest_Click");
                AirbridgeUnity.TrackEvent(evt);

                startButtonContainer.SetActive(false);
                KeyManager.Instance.SignIn(pk);
                Game.Game.instance.IsGuestLogin = true;
            });
            guestButton.interactable = true;
            _isSetGuestPrivateKey = true;
        }

#if APPLY_MEMORY_IOS_OPTIMIZATION || RUN_ON_MOBILE
        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();

            MainCanvas.instance.RemoveWidget(this);
        }
#endif

        private void ApplyPlanetContext(PlanetContext planetContext)
        {
            NcDebug.Log("[IntroScreen] ApplyPlanetRegistry invoked.");
            selectPlanetScroll.SetData(
                planetContext?.PlanetRegistry,
                planetContext?.SelectedPlanetInfo?.ID);

            ApplySelectedPlanetInfo(planetContext);
            ApplySelectedPlanetAccountInfo(planetContext);
        }

        private void ApplySelectedPlanetInfo(PlanetContext planetContext)
        {
            NcDebug.Log("[IntroScreen] ApplySelectedPlanetInfo invoked.");
            var planetInfo = planetContext?.SelectedPlanetInfo;
            if (planetInfo is null)
            {
                NcDebug.Log("[IntroScreen] ApplySelectedPlanetInfo... planetInfo is null");
                yourPlanetButtonText.text = "Null";
                planetAccountInfoText.text = string.Empty;
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            yourPlanetButtonText.text = textInfo.ToTitleCase(planetInfo.Name);
        }

        private void ApplySelectedPlanetAccountInfo(PlanetContext planetContext)
        {
            NcDebug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo invoked.");
            var planetAccountInfo = planetContext?.SelectedPlanetAccountInfo;
            if (planetAccountInfo?.AgentAddress is null)
            {
                NcDebug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo... planetAccountInfo?.AgentAddress is null.");
                planetAccountInfoText.text = SigninContext.HasLatestSignedInSocialType
                    ? L10nManager.Localize("SDESC_THERE_IS_NO_ACCOUNT")
                    : string.Empty;

                return;
            }

            if (!(planetAccountInfo.IsAgentPledged.HasValue &&
                  planetAccountInfo.IsAgentPledged.Value))
            {
                NcDebug.Log("[IntroScreen] ApplySelectedPlanetAccountInfo... planetAccountInfo.IsAgentPledged is false.");
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

            NcDebug.Log($"[IntroScreen] google.State.Value: {google.State.Value}");
            switch (google.State.Value)
            {
                case GoogleSigninBehaviour.SignInState.Signed:
                    NcDebug.Log("[IntroScreen] Already signed in google. Anyway, invoke OnGoogleSignedIn.");
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Google);
                    OnSocialSignedIn.OnNext((SigninContext.SocialType.Google, google.Email, google.IdToken));
                    return;
                case GoogleSigninBehaviour.SignInState.Waiting:
                    NcDebug.Log("[IntroScreen] Already waiting for google sign in.");
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

            NcDebug.Log($"[IntroScreen] apple.State.Value: {apple.State.Value}");
            switch (apple.State.Value)
            {
                case AppleSigninBehaviour.SignInState.Signed:
                    NcDebug.Log("[IntroScreen] Already signed in apple. Anyway, invoke OnAppleSignedIn.");
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Apple);
                    OnSocialSignedIn.OnNext((SigninContext.SocialType.Apple, apple.Email, apple.IdToken));
                    return;
                case AppleSigninBehaviour.SignInState.Waiting:
                    NcDebug.Log("[IntroScreen] Already waiting for apple sign in.");
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

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
using Nekoyume.AssetBundleHelper;
using Nekoyume.Game.Controller;
using Nekoyume.Game.OAuth;
using Nekoyume.GraphQL.GraphTypes;
using Nekoyume.L10n;
using Nekoyume.Planet;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using UniRx;

    public class IntroScreen : ScreenWidget
    {
        [Serializable]
        public struct AgentInfo
        {
            private const string AccountTextFormat =
                "<color=#B38271>Lv. {0}</color> {1} <color=#A68F7E>#{2}</color>"; 
            public PlanetId? PlanetId { get; private set; }
            public TextMeshProUGUI title;
            public GameObject noAccount;
            public Button noAccountCreateButton;
            public GameObject account;
            public TextMeshProUGUI[] accountTexts;
            public Button accountImportKeyButton;

            public void Set(
                PlanetContext planetContext,
                PlanetAccountInfo planetAccountInfo)
            {
                if (planetContext?.Planets is null ||
                    planetAccountInfo is null)
                {
                    Debug.LogError("[IntroScreen] AgentInfo.Set()... context(planets)" +
                                   " or info is null");
                    return;
                }

                PlanetId = planetAccountInfo.PlanetId;
                if (planetContext.Planets.TryGetPlanetInfoById(PlanetId.Value, out var planetInfo))
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
                                text.text = "Empty";
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

                    noAccount.SetActive(false);
                    account.SetActive(true);

                    // FIXME: Subscription does not disposed.
                    accountImportKeyButton.OnClickAsObservable()
                        .First()
                        .Subscribe(_ =>
                        {
                            PlanetSelector.SelectPlanetById(planetContext, planetInfo.ID);
                            PlanetSelector.SelectPlanetAccountInfo(
                                planetContext,
                                planetAccountInfo.PlanetId);
                        });
                }
            }
        }

        // NOTE: Use to mobileContainerAnimator. 
        private static readonly int IdleToShowButtons = Animator.StringToHash("IdleToShowButtons");

        [SerializeField] private GameObject pcContainer;

        [Header("Mobile")]

        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private Animator mobileContainerAnimator;
        [SerializeField] private RawImage videoImage;

        // NOTE: `startButtonContainer` enabled automatically by animator when
        //        the `mobileContainer` is enabled.
        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private ConditionalButton startButton;
        [SerializeField] private Button signinButton;
        [SerializeField] private Button guestButton;
        [SerializeField] private Button planetButton;
        [SerializeField] private TextMeshProUGUI planetText;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;
        [SerializeField] private CodeReaderView codeReaderView;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button videoSkipButton;

        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button appleSignInButton;

        [SerializeField] private GameObject selectPlanetPopup;
        [SerializeField] private ConditionalButton heimdallButton;
        [SerializeField] private ConditionalButton odinButton;

        [SerializeField] private GameObject planetAccountInfosPopup;
        [SerializeField] private AgentInfo planetAccountInfoLeft;
        [SerializeField] private AgentInfo planetAccountInfoRight;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;
        private PlanetContext _planetContext;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";

        public Subject<(IntroScreen introScreen, GoogleSigninBehaviour googleSigninBehaviour)>
            OnClickGoogleSignIn { get; } = new();
        public Subject<(IntroScreen introScreen, GoogleSigninBehaviour googleSigninBehaviour)>
            OnGoogleSignedIn { get; } = new();

        public Subject<(IntroScreen introScreen, AppleSigninBehaviour appleSigninBehaviour)>
            OnClickAppleSignIn { get; } = new();
        public Subject<(IntroScreen introScreen, AppleSigninBehaviour appleSigninBehaviour)>
            OnAppleSignedIn { get; } = new();

        protected override void Awake()
        {
            base.Awake();

            startButton.OnSubmitSubject.Subscribe(_ =>
            {
                if (Find<LoginSystem>().KeyStore.ListIds().Any())
                {
                    startButtonContainer.SetActive(false);
                    Find<LoginSystem>().Show(_keyStorePath, _privateKey);
                }
                else
                {
                    startButton.Interactable = false;
                    Game.Game.instance.PortalConnect.OpenPortal();
                    Analyzer.Instance.Track("Unity/Intro/SocialLogin_open");

                    StartCoroutine(CoSocialLogin());
                }
            }).AddTo(gameObject);
            googleSignInButton.onClick.AddListener(() =>
            {
                Debug.Log("[IntroScreen] Click google sign in button.");
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Click");
                if (!Game.Game.instance.TryGetComponent<GoogleSigninBehaviour>(out var google))
                {
                    google = Game.Game.instance.gameObject.AddComponent<GoogleSigninBehaviour>();
                }

                OnClickGoogleSignIn.OnNext((this, google));

                Debug.Log($"[IntroScreen] google.State.Value: {google.State.Value}");
                if (google.State.Value is not (
                    GoogleSigninBehaviour.SignInState.Signed or
                    GoogleSigninBehaviour.SignInState.Waiting))
                {
                    google.OnSignIn();
                    startButtonContainer.SetActive(false);
                    googleSignInButton.gameObject.SetActive(false);
                    google.State
                        .SkipLatestValueOnSubscribe()
                        .First()
                        .Subscribe(state =>
                        {
                            var isCanceled = state is GoogleSigninBehaviour.SignInState.Canceled;
                            startButtonContainer.SetActive(isCanceled);
                            googleSignInButton.gameObject.SetActive(isCanceled);
                            if (state is GoogleSigninBehaviour.SignInState.Signed)
                            {
                                OnGoogleSignedIn.OnNext((this, google));    
                            }
                        });
                }
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

                OnClickAppleSignIn.OnNext((this, apple));

                if (apple.State.Value is not (
                    AppleSigninBehaviour.SignInState.Signed or
                    AppleSigninBehaviour.SignInState.Waiting))
                {
                    apple.OnSignIn();
                    startButtonContainer.SetActive(false);
                    appleSignInButton.gameObject.SetActive(false);
                    apple.State
                        .SkipLatestValueOnSubscribe()
                        .First()
                        .Subscribe(state =>
                        {
                            var isCanceled = state is AppleSigninBehaviour.SignInState.Canceled;
                            startButtonContainer.SetActive(isCanceled);
                            appleSignInButton.gameObject.SetActive(isCanceled);
                            if (state is AppleSigninBehaviour.SignInState.Signed)
                            {
                                OnAppleSignedIn.OnNext((this, apple));    
                            }
                        });
                }
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
            planetButton.onClick.AddListener(() => selectPlanetPopup.SetActive(true));
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

            startButton.Interactable = true;
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
            OnClickGoogleSignIn.Dispose();
            OnGoogleSignedIn.Dispose();
            OnClickAppleSignIn.Dispose();
            OnAppleSignedIn.Dispose();
        }

        public void Show(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            Analyzer.Instance.Track("Unity/Intro/Show");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            _planetContext = planetContext;

#if RUN_ON_MOBILE
            ApplyCurrentPlanetInfo(_planetContext);
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
            if (mobileContainerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash
                != IdleToShowButtons)
            {
                mobileContainerAnimator.Play("Idle", 0);
                mobileContainerAnimator.SetTrigger(IdleToShowButtons);    
            }

            // videoImage.gameObject.SetActive(false);
            startButtonContainer.SetActive(false);
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
            if (mobileContainerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash
                != IdleToShowButtons)
            {
                mobileContainerAnimator.Play("Idle");
                mobileContainerAnimator.SetTrigger(IdleToShowButtons);    
            }

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

        public void ShowPlanetAccountInfosPopup(PlanetContext planetContext)
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
                    info.PlanetId.Equals(PlanetId.OdinInternal)));
            planetAccountInfoRight.Set(
                planetContext,
                planetContext.PlanetAccountInfos?.FirstOrDefault(info =>
                    info.PlanetId.Equals(PlanetId.Heimdall) ||
                    info.PlanetId.Equals(PlanetId.HeimdallInternal)));
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

        private IEnumerator CoSocialLogin()
        {
            yield return new WaitForSeconds(180);

            startButton.Interactable = true;
            yield break;
            Analyzer.Instance.Track("Unity/Intro/SocialLogin_1");
            var popup = Find<TitleOneButtonSystem>();
            popup.Show("UI_LOGIN_ON_BROWSER_TITLE","UI_LOGIN_ON_BROWSER_CONTENT");
            popup.SubmitCallback = null;
            popup.SubmitCallback = () =>
            {
                Game.Game.instance.PortalConnect.OpenPortal(() => popup.Close());
                Analyzer.Instance.Track("Unity/Intro/SocialLogin_2");
            };

            yield return new WaitForSeconds(1);
            Game.Game.instance.PortalConnect.OpenPortal(() => popup.Close());
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
            var planets = planetContext.Planets;
            var planetInfo = planetContext.SelectedPlanetInfo;
            if (planets is null ||
                planetInfo is null)
            {
                planetText.text = "Null";
                heimdallButton.Interactable = false;
                heimdallButton.Text = "Heimdall (Null)";
                odinButton.Interactable = false;
                odinButton.Text = "Odin (Null)";
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            planetText.text = textInfo.ToTitleCase(planetInfo.Name);

            if (planets.TryGetPlanetInfoById(PlanetId.Heimdall, out var heimdallInfo) ||
                planets.TryGetPlanetInfoById(PlanetId.HeimdallInternal, out heimdallInfo))
            {
                heimdallButton.Text = textInfo.ToTitleCase(heimdallInfo.Name);
            }

            if (planets.TryGetPlanetInfoById(PlanetId.Odin, out var odinInfo) ||
                planets.TryGetPlanetInfoById(PlanetId.OdinInternal, out odinInfo))
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

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
                            // FIXME: Handle planetContext.Error.
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

        [SerializeField] private GameObject touchScreenButtonGO;
        [SerializeField] private Button touchScreenButton;

        // NOTE: `startButtonContainer` will be enabled when the mobileContainerAnimator
        //       is in the `IdleToShowButtons` state.
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

        public Subject<(string email, string idToken)> OnGoogleSignedIn { get; } = new();

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

                Debug.Log($"[IntroScreen] google.State.Value: {google.State.Value}");
                switch (google.State.Value)
                {
                    case GoogleSigninBehaviour.SignInState.Signed:
                        Debug.Log("[IntroScreen] Already signed in google. Anyway, invoke OnGoogleSignedIn.");
                        OnGoogleSignedIn.OnNext((google.Email, google.IdToken));
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
                                OnGoogleSignedIn.OnNext((google.Email, google.IdToken));
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
            GetGuestPrivateKey();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnGoogleSignedIn.Dispose();
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

        private void ApplyCurrentPlanetInfo(PlanetContext planetContext)
        {
            var planetRegistry = planetContext.PlanetRegistry;
            var planetInfo = planetContext.SelectedPlanetInfo;
            if (planetRegistry is null ||
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

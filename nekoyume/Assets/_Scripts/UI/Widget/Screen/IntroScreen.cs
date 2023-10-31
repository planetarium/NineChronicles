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
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using UniRx;

    public class IntroScreen : ScreenWidget
    {
        [SerializeField] private GameObject pcContainer;
        [Header("Mobile")]
        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private RawImage videoImage;

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

        [SerializeField]
        private Button googleSignInButton;

        [SerializeField] private GameObject selectPlanetPopup;
        [SerializeField] private ConditionalButton heimdallButton;
        [SerializeField] private ConditionalButton odinButton;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;
        private PlanetContext _planetContext;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";
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
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Click");
                if (!Game.Game.instance.TryGetComponent<GoogleSigninBehaviour>(out var google))
                {
                    google = Game.Game.instance.gameObject.AddComponent<GoogleSigninBehaviour>();
                }

                if (google.State.Value is not (GoogleSigninBehaviour.SignInState.Signed or
                    GoogleSigninBehaviour.SignInState.Waiting))
                {
                    google.OnSignIn();
                    startButtonContainer.SetActive(false);
                    google.State
                        .SkipLatestValueOnSubscribe()
                        .First()
                        .Subscribe(state =>
                        {
                            startButtonContainer.SetActive(
                                state is GoogleSigninBehaviour.SignInState.Canceled);
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

        public void Show(string keyStorePath, string privateKey, PlanetContext planetContext)
        {
            Analyzer.Instance.Track("Unity/Intro/Show");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            _planetContext = planetContext;

#if UNITY_ANDROID
            ApplyCurrentPlanetInfo(_planetContext);
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
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

        public void Show()
        {
            pcContainer.SetActive(false);
            mobileContainer.SetActive(true);
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

using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet.Common;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.Game.OAuth;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using UniRx;

    public class IntroScreen : LoadingScreen
    {
        [SerializeField] private GameObject pcContainer;
        [Header("Mobile")]
        [SerializeField] private GameObject mobileContainer;
        // [SerializeField] private Button touchScreenButton;
        [SerializeField] private RawImage videoImage;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private ConditionalButton startButton;
        [SerializeField] private Button signinButton;
        [SerializeField] private Button guestButton;

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

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;

        private const string GuestPrivateKeyUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/guest-pk";
        protected override void Awake()
        {
            base.Awake();
            indicator.Close();

            // videoPlayer.loopPointReached += _ => OnVideoEnd();
            // videoSkipButton.onClick.AddListener(OnVideoEnd);

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

            startButton.Interactable = true;
            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            videoSkipButton.interactable = true;
            googleSignInButton.interactable = true;
            GetGuestPrivateKey();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            Analyzer.Instance.Track("Unity/Intro/Show");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;

#if UNITY_ANDROID
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

        private void ShowMobile()
        {
            // PlayerPrefs FirstPlay
            // if (PlayerPrefs.GetInt("FirstPlay", 0) == 0)
            // {
            //     PlayerPrefs.SetInt("FirstPlay", 1);
            //     PlayerPrefs.Save();
            //
            //     videoImage.gameObject.SetActive(true);
            //     videoSkipButton.gameObject.SetActive(false);
            //     videoPlayer.Play();
            //     Analyzer.Instance.Track("Unity/Intro/Video/Start");
            //
            //     yield return new WaitForSeconds(5);
            //
            //     videoSkipButton.gameObject.SetActive(true);
            // }
            // else
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            Analyzer.Instance.Track("Unity/Intro/StartButton/Show");
            startButtonContainer.SetActive(true);
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
                Find<GrayLoadingScreen>().ShowProgress(GameInitProgress.CompleteLogin);
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
    }
}

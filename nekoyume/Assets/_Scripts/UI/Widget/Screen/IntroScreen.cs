using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        [Header("Mobile")]
        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private Button touchScreenButton;
        [SerializeField] private RawImage videoImage;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button startButton;
        [SerializeField] private Button signinButton;

        [SerializeField] private GameObject socialLoginContainer;
        [SerializeField] private CapturedImage socialLoginBackground;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button twitterLoginButton;
        [SerializeField] private Button discordLoginButton;
        [SerializeField] private Button appleLoginButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;

        [SerializeField] private LoadingIndicator mobileIndicator;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private SocialLogin socialLogin;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            mobileIndicator.Close();

            touchScreenButton.onClick.AddListener(() =>
            {
                touchScreenButton.gameObject.SetActive(false);

                var keystore = Find<LoginSystem>().KeyStore;
                if (keystore.ListIds().Any())
                {
                    Find<LoginSystem>().Show(_keyStorePath, _privateKey);
                }
                else
                {
                    startButtonContainer.SetActive(true);
                }
            });
            startButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                socialLoginBackground.Show();
                socialLoginContainer.SetActive(true);
                videoImage.gameObject.SetActive(true);
                videoPlayer.Play();
            });
            signinButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
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

            googleLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                mobileIndicator.Show("Sign in...");
                socialLogin.Signin(() =>
                {
                    Find<LoginSystem>().Show(_keyStorePath, _privateKey);
                });
            });
            twitterLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                mobileIndicator.Show("Sign in...");

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });
            discordLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                mobileIndicator.Show("Sign in...");

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });
            appleLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                mobileIndicator.Show("Sign in...");

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });

            videoPlayer.loopPointReached += _ => videoImage.gameObject.SetActive(false);

            touchScreenButton.interactable = true;
            startButton.interactable = true;
            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            googleLoginButton.interactable = true;
            twitterLoginButton.interactable = true;
            discordLoginButton.interactable = true;
            appleLoginButton.interactable = true;
        }

        public void Show(string keyStorePath, string privateKey)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);

            if (Platform.IsMobilePlatform())
            {
                mobileContainer.SetActive(true);
                videoImage.gameObject.SetActive(false);
                startButtonContainer.SetActive(false);
                socialLoginContainer.SetActive(false);
                qrCodeGuideContainer.SetActive(false);
                mobileIndicator.Close();
            }
            else
            {
                mobileContainer.SetActive(false);

                indicator.Show("Verifying transaction..");
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
        }

        private void ShowQrCodeGuide()
        {
            if (_guideIndex >= GuideCount)
            {
                qrCodeGuideContainer.SetActive(false);

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
            else
            {
                qrCodeGuideImages[_guideIndex].SetActive(true);
                qrCodeGuideText.text = L10nManager.Localize($"INTRO_QR_CODE_GUIDE_{_guideIndex}");
            }
        }
    }
}

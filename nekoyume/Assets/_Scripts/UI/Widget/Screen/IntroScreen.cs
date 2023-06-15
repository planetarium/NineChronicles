using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        [Header("Mobile")]
        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private GameObject logoContainer;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button startButton;
        [SerializeField] private Button signinButton;

        [SerializeField] private GameObject socialLoginContainer;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button twitterLoginButton;
        [SerializeField] private Button discordLoginButton;
        [SerializeField] private Button appleLoginButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;

        [SerializeField] private GrayLoadingScreen grayLoadingScreen;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();

            startButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                socialLoginContainer.SetActive(true);
            });
            signinButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
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
        }

        public void Show(string keyStorePath, string privateKey)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);

            if (Platform.IsMobilePlatform())
            {
                mobileContainer.SetActive(true);
                logoContainer.SetActive(true);
                startButtonContainer.SetActive(false);
                socialLoginContainer.SetActive(false);
                qrCodeGuideContainer.SetActive(false);
                grayLoadingScreen.Close();
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

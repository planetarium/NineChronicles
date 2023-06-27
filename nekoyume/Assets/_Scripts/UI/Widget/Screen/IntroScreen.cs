using System.Collections;
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
        // [SerializeField] private Button touchScreenButton;
        [SerializeField] private RawImage videoImage;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button startButton;
        [SerializeField] private Button signinButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;

        [SerializeField] private LoadingIndicator mobileIndicator;
        [SerializeField] private VideoPlayer videoPlayer;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            mobileIndicator.Close();

            videoPlayer.loopPointReached += _ => OnVideoEnd();

            startButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
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

            startButton.interactable = true;
            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
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
                qrCodeGuideContainer.SetActive(false);
                mobileIndicator.Close();

                StartCoroutine(CoShowMobile());
            }
            else
            {
                mobileContainer.SetActive(false);

                indicator.Show("Verifying transaction..");
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
        }

        private IEnumerator CoShowMobile()
        {
            yield return new WaitForSeconds(2);

            // PlayerPrefs FirstPlay
            if (PlayerPrefs.GetInt("FirstPlay", 0) == 0)
            {
                PlayerPrefs.SetInt("FirstPlay", 1);
                PlayerPrefs.Save();

                videoImage.gameObject.SetActive(true);
                videoPlayer.Play();
            }

            var keystore = Find<LoginSystem>().KeyStore;
            if (keystore.ListIds().Any())
            {
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
            else
            {
                startButtonContainer.SetActive(true);
            }
        }

        private void OnVideoEnd()
        {
            videoImage.gameObject.SetActive(false);
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

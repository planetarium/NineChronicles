using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.KeyStore;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        [SerializeField] private Button guestButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;
        [SerializeField] private DataMatrixReaderView codeReaderView;

        [SerializeField] private LoadingIndicator mobileIndicator;
        [SerializeField] private VideoPlayer videoPlayer;

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
            mobileIndicator.Close();

            videoPlayer.loopPointReached += _ => OnVideoEnd();

            startButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
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

            startButton.interactable = true;
            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            GetGuestPrivateKey();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            Analyzer.Instance.Track("Unity/Intro/Show");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;

#if UNITY_ANDROID
            mobileContainer.SetActive(true);
            videoImage.gameObject.SetActive(false);
            startButtonContainer.SetActive(false);
            qrCodeGuideContainer.SetActive(false);
            mobileIndicator.Close();
            StartCoroutine(CoShowMobile());
#else
            mobileContainer.SetActive(false);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            indicator.Show("Verifying transaction..");
            Find<LoginSystem>().Show(_keyStorePath, _privateKey);
#endif
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
                Analyzer.Instance.Track("Unity/Intro/Video/Start");
            }
            else
            {
                AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            }

            Analyzer.Instance.Track("Unity/Intro/StartButton/Show");
            startButtonContainer.SetActive(true);
            signinButton.gameObject.SetActive(!Find<LoginSystem>().KeyStore.List().Any());
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
                Debug.LogException(e);
                return;
            }

            guestButton.gameObject.SetActive(true);
            guestButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                Find<LoginSystem>().Show(_keyStorePath, pk);
                Find<GrayLoadingScreen>().Show("UI_LOAD_WORLD", true);
            });
            guestButton.interactable = true;
        }
    }
}

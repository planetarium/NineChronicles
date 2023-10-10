using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;
    public class LoadingScreen : ScreenWidget
    {
        public enum LoadingType
        {
            None,
            Entering,
            Adventure,
            Arena,
            Shop,
            Workshop,
        }

        [Serializable]
        private struct BackgroundItem
        {
            public LoadingType type;
            public VideoClip videoClip;
            public Texture texture;
        }

        [SerializeField] private LoadingIndicator indicator;
        [SerializeField] private TextMeshProUGUI toolTip;
        [SerializeField] private Button toolTipChangeButton;
        [SerializeField] private Slider slider;

        [SerializeField] private GameObject animationContainer;
        [SerializeField] private RawImage imageContainer;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Texture videoTexture;

        [SerializeField] private BackgroundItem[] backgroundItems;

        private string _defaultMessage;
        private readonly ReactiveProperty<string> _message = new();
        private string[] _tips;
        private Tweener _tweener;
        private Coroutine _coroutine;

        protected override void Awake()
        {
            base.Awake();

            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                LoadL10N();
            }

            var pos = transform.localPosition;
            pos.z = -5f;
            transform.localPosition = pos;

            if (ReferenceEquals(indicator, null) || ReferenceEquals(toolTip, null))
            {
                throw new SerializeFieldNullException();
            }

            _message.Subscribe(indicator.Show).AddTo(gameObject);
            if (toolTipChangeButton != null)
            {
                toolTipChangeButton.onClick.AddListener(SetToolTipText);
            }

            L10nManager.OnLanguageChange.Subscribe(_ => LoadL10N()).AddTo(gameObject);
        }

        public void Show(
            LoadingType loadingType = LoadingType.None,
            string message = null,
            bool autoClose = false,
            bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _message.Value = !string.IsNullOrEmpty(message) ? message : _defaultMessage;

            Find<HeaderMenuStatic>().Close();

            SetBackGround(loadingType);
            SetToolTipText();
            PlaySliderAnimation();

            if (autoClose)
            {
                Observable.Timer(TimeSpan.FromSeconds(3))
                    .Subscribe(_ => Close()).AddTo(gameObject);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopSliderAnimation();
        }

        private void LoadL10N()
        {
            _defaultMessage = $"{L10nManager.Localize("BLOCK_CHAIN_MINING_TX")}...";
            _tips = L10nManager.LocalizePattern("^UI_TIPS_[0-9]+$").Values.ToArray();
        }

        private void SetToolTipText()
        {
            if (_tips != null)
            {
                toolTip.text = _tips[Random.Range(0, _tips.Length)];
            }
        }

        private void PlaySliderAnimation()
        {
            StopSliderAnimation();

            _coroutine = StartCoroutine(CoPlaySliderAnimation());
        }

        private IEnumerator CoPlaySliderAnimation()
        {
            yield return new WaitForSeconds(1f);

            while (gameObject.activeSelf)
            {
                slider.value = slider.minValue;
                _tweener = slider.DOValue(slider.maxValue, Random.Range(1f, 3f));

                yield return _tweener.WaitForCompletion();
            }
        }

        private void StopSliderAnimation()
        {
            if (_tweener is not null && _tweener.IsActive() && _tweener.IsPlaying())
            {
                _tweener.Kill();
                _tweener = null;
            }

            if (_coroutine is not null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private void SetBackGround(LoadingType type)
        {
            var playVideo = type != LoadingType.None;
            animationContainer.SetActive(!playVideo);
            imageContainer.gameObject.SetActive(playVideo);

            if (playVideo)
            {
                var item = backgroundItems.FirstOrDefault(item => item.type == type);
                var clip = item.videoClip;

                if (clip)
                {
                    videoPlayer.clip = clip;
                    videoPlayer.Play();
                    imageContainer.texture = videoTexture;
                }
                else
                {
                    imageContainer.texture = item.texture;
                }
            }
        }
    }
}

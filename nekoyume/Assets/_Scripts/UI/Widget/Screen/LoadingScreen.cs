using System.Linq;
using DG.Tweening;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoadingScreen : ScreenWidget
    {
        [SerializeField] private LoadingIndicator indicator;
        [SerializeField] private TextMeshProUGUI toolTip;
        [SerializeField] private Button toolTipChangeButton;
        [SerializeField] private Slider slider;

        private string _defaultMessage;
        private readonly ReactiveProperty<string> _message = new();
        private string[] _tips;
        private Tweener _tweener;

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

        public void Show(string message = null, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            _message.Value = !string.IsNullOrEmpty(message) ? message : _defaultMessage;

            Find<HeaderMenuStatic>().Close();

            SetToolTipText();
            PlaySliderAnimation();
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

            slider.value = slider.minValue;
            _tweener = slider.DOValue(slider.maxValue, Random.Range(1f, 2f)).SetDelay(1f);
        }

        private void StopSliderAnimation()
        {
            if (_tweener is not null && _tweener.IsActive() && _tweener.IsPlaying())
            {
                _tweener.Kill();
                _tweener = null;
            }
        }
    }
}

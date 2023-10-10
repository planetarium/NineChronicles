using System.Collections;
using System.Linq;
using DG.Tweening;
using Nekoyume.L10n;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class LoadingModule : MonoBehaviour
    {
        [SerializeField] private LoadingIndicator indicator;
        [SerializeField] private TextMeshProUGUI toolTip;
        [SerializeField] private Button toolTipChangeButton;
        [SerializeField] private Slider slider;

        private string _defaultMessage;
        private readonly ReactiveProperty<string> _message = new();
        private string[] _tips;
        private Tweener _tweener;
        private Coroutine _coroutine;

        public void Initialize()
        {
            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                LoadL10N();
            }

            L10nManager.OnLanguageChange.Subscribe(_ => LoadL10N()).AddTo(gameObject);

            if (ReferenceEquals(indicator, null) || ReferenceEquals(toolTip, null))
            {
                throw new SerializeFieldNullException();
            }

            _message.Subscribe(indicator.Show).AddTo(gameObject);
            if (toolTipChangeButton != null)
            {
                toolTipChangeButton.onClick.AddListener(SetToolTipText);
            }
        }

        private void LoadL10N()
        {
            _defaultMessage = $"{L10nManager.Localize("BLOCK_CHAIN_MINING_TX")}...";
            _tips = L10nManager.LocalizePattern("^UI_TIPS_[0-9]+$").Values.ToArray();
        }

        public void SetMessage(string message)
        {
            _message.Value = !string.IsNullOrEmpty(message) ? message : _defaultMessage;
        }

        public void SetToolTipText()
        {
            if (_tips != null)
            {
                toolTip.text = _tips[Random.Range(0, _tips.Length)];
            }
        }

        public void PlaySliderAnimation()
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
                _tweener = slider.DOValue(slider.maxValue, Random.Range(0.05f, 1.5f));

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

        protected void OnDisable()
        {
            StopSliderAnimation();
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UniRx;
using DG.Tweening;

namespace Nekoyume.UI
{
    public class ComboText : MonoBehaviour
    {
        private const float TweenDuration = 0.3f;
        private const float DelayTime = 1.4f;

        private static readonly Vector3 LocalScaleBefore = new Vector3(1.5f, 1.5f, 1f);
        private static readonly Vector3 LocalScaleAfter = new Vector3(1f, 1f, 1f);

        public TextMeshProUGUI shadowText;
        public TextMeshProUGUI labelText;
        public CanvasGroup group;

        public int _combo { get; private set; }
        private Subject<int> comboSubject = new Subject<int>();
        [System.NonSerialized] public int comboMax;

        private RectTransform _rectTransform;
        private Sequence _sequence;
        private Coroutine _coroutine;

        private void Awake()
        {
            comboSubject.SubscribeTo(shadowText).AddTo(gameObject);
            comboSubject.SubscribeTo(labelText).AddTo(gameObject);
            _rectTransform = GetComponent<RectTransform>();

            _sequence = DOTween.Sequence();
            _sequence
                .SetAutoKill(false)
                .OnPlay(() =>
                {
                    _rectTransform.localScale = LocalScaleBefore;
                    group.alpha = 1f;
                })
                .Insert(0, _rectTransform.DOScale(LocalScaleAfter, TweenDuration)
                    .SetEase(Ease.OutCubic))
                .Join(group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration)
                    .SetEase(Ease.InCirc));
        }

        public void Show(bool attacked)
        {
            if (attacked)
            {
                if (++_combo > comboMax)
                    _combo = 1;

                comboSubject.OnNext(_combo);
                gameObject.SetActive(true);

                _sequence?.Complete();

                _sequence = DOTween.Sequence();
                _sequence
                    .SetAutoKill(false)
                    .OnPlay(() =>
                    {
                        _rectTransform.localScale = LocalScaleBefore;
                        group.alpha = 1f;
                    })
                    .OnComplete(() =>
                    {
                        _sequence = null;
                    })
                    .Insert(0, _rectTransform.DOScale(LocalScaleAfter, TweenDuration)
                        .SetEase(Ease.OutCubic))
                    .Join(group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration)
                        .SetEase(Ease.InCirc));

                _sequence.Play();

                if(_coroutine != null)
                    StopCoroutine(_coroutine);
                _coroutine = StartCoroutine(CoClose());
            }
            else
            {
                _combo = 0;
            }
        }

        private IEnumerator CoClose()
        {
            yield return new WaitForSeconds(DelayTime);

            gameObject.SetActive(false);
        }

        public void Close()
        {
            _combo = 0;
            gameObject.SetActive(false);
        }
    }
}

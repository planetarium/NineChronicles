using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UniRx;
using DG.Tweening;
using UnityEngine.UI;

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

        private int _combo = 0;
        private Subject<int> comboSubject = new Subject<int>();
        [System.NonSerialized] public int comboMax;

        private RectTransform _rectTransform;

        private void Awake()
        {
            comboSubject.SubscribeTo(shadowText).AddTo(gameObject);
            comboSubject.SubscribeTo(labelText).AddTo(gameObject);
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Show(bool attacked)
        {
            if (attacked)
            {
                if (++_combo > comboMax)
                    _combo = 1;

                if (_combo == comboMax)
                {
                    CutSceneTest.Show(CutSceneTest.AnimationType.Type5);
                }

                group.alpha = 1f;
                comboSubject.OnNext(_combo);
                gameObject.SetActive(true);

                _rectTransform.localScale = LocalScaleBefore;

                _rectTransform.DOScale(LocalScaleAfter, TweenDuration).SetEase(Ease.OutCubic);
                group.DOFade(0.0f, TweenDuration * 2.0f).SetDelay(TweenDuration).SetEase(Ease.InCirc);

                StartCoroutine(CoClose());
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
            gameObject.SetActive(false);
        }
    }
}

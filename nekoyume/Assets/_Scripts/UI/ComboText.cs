using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UniRx;
using DG.Tweening;

namespace Nekoyume.UI
{
    public class ComboText : MonoBehaviour
    {
        public TextMeshProUGUI shadowText;
        public TextMeshProUGUI meshText;
        public float delayTime;
        private ReactiveProperty<int> _combo = new ReactiveProperty<int>();

        private void Awake()
        {
            _combo.SubscribeTo(shadowText).AddTo(gameObject);
            _combo.SubscribeTo(meshText).AddTo(gameObject);
        }

        public void Show(int combo)
        {
            _combo.Value = combo;
            if (_combo.Value > 0)
            {
                gameObject.SetActive(true);
                Sequence seq = DOTween.Sequence();
                GetComponent<RectTransform>().DOAnchorPosX(-200f, 0.3f).From();
                StartCoroutine(CoClose());
            }
            else
            {
                StopAllCoroutines();
                gameObject.SetActive(false);
            }
        }

        private IEnumerator CoClose()
        {
            yield return new WaitForSeconds(delayTime);
            gameObject.SetActive(false);
        }
    }
}
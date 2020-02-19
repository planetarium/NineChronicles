using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

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

        public void Set(bool attacked)
        {
            _combo.Value = attacked ? _combo.Value + 1 : 0;
        }

        public void Show()
        {
            if (_combo.Value > 1)
            {
                gameObject.SetActive(true);
                StartCoroutine(CoClose());
            }
        }

        private IEnumerator CoClose()
        {
            yield return new WaitForSeconds(delayTime);
            gameObject.SetActive(false);
        }
    }
}
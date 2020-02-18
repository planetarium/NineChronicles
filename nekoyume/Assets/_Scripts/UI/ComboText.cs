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

        public void Set(string combo)
        {
            shadowText.text = combo;
            meshText.text = combo;
        }

        public void Close()
        {
            StartCoroutine(CoClose());
        }

        private IEnumerator CoClose()
        {
            yield return new WaitForSeconds(delayTime);
            gameObject.SetActive(false);
        }
    }
}
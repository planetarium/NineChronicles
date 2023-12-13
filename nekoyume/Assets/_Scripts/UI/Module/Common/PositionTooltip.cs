using System.Collections;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    public class PositionTooltip : MonoBehaviour
    {
        [SerializeField]
        private bool disableWhenTouchScreen = true;

        [SerializeField]
        protected TextMeshProUGUI titleText;

        [SerializeField]
        protected TextMeshProUGUI contentText;

        private void OnEnable()
        {
            if (disableWhenTouchScreen)
            {
                StartCoroutine(CoUpdate());
            }
        }

        private void OnDisable()
        {
            if (disableWhenTouchScreen)
            {
                StopCoroutine(CoUpdate());
            }
        }

        private IEnumerator CoUpdate()
        {
            while (true)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    gameObject.SetActive(false);
                }

                yield return null;
            }
        }

        public void Set(string title, string content)
        {
            if (titleText != null)
                titleText.text = title;

            if (content != null)
                contentText.text = content;
        }
    }
}

using System.Collections;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class SimpleTooltip : Widget
    {
        [SerializeField] private TextMeshProUGUI tooltipText;

        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }

        public void Show(ItemBase itemBase, Transform target)
        {
            Show(itemBase.GetLocalizedDescription(), target);
        }

        public void Show(string message, Transform target)
        {
            tooltipText.text = message;
            _rectTransform.position = target.position;
            gameObject.SetActive(true);
            StartCoroutine(CoWaitClose());
            base.Show();
        }

        private IEnumerator CoWaitClose()
        {
            while (Input.GetMouseButtonDown(0))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Close();
                    yield break;
                }

                yield return null;
            }
        }
    }
}

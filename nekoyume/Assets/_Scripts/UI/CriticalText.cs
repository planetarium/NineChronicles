using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CriticalText : HudWidget
    {
        private static RectTransform CanvasRect;

        public TextMeshProUGUI label;
        public TextMeshProUGUI shadow;

        private Vector3 _force;
        private Vector3 _addForce;

        public static CriticalText Show(Vector3 position, Vector3 force, string text)
        {
            var popupText = Create<CriticalText>(true);
            popupText.label.text = text;
            popupText.shadow.text = text;

            if (CanvasRect == null)
                CanvasRect = popupText.transform.root.gameObject.GetComponent<RectTransform>();

            RectTransform rectTransform = popupText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = CalcCanvasPosition(position);
            rectTransform.localScale = Vector3.one * 0.7f;
            var pos =  CalcCanvasPosition(position + force);
            rectTransform.DOMove(pos, 0.3f).SetEase(Ease.OutElastic);
            rectTransform.DOScale(Vector3.one * 1.2f, 0.3f).SetEase(Ease.OutElastic);
            
            Destroy(popupText.gameObject, 0.8f);

            return popupText;
        }

        private static Vector2 CalcCanvasPosition(Vector3 position)
        {
            if (CanvasRect == null)
                return position;
            Vector2 viewportPosition = Camera.main.WorldToViewportPoint(position);
            return new Vector2(
                ((viewportPosition.x * CanvasRect.sizeDelta.x)),
                ((viewportPosition.y * CanvasRect.sizeDelta.y)));
        }
    }
}

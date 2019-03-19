using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class DamageText : HUD
    {
        private static RectTransform CanvasRect;

        public TextMeshProUGUI label;

        private Vector3 _force;
        private Vector3 _addForce;

        public static DamageText Show(Vector3 position, Vector3 force, string text)
        {
            var popupText = Create<DamageText>(true);
            popupText.label.text = text;

            if (CanvasRect == null)
                CanvasRect = popupText.transform.root.gameObject.GetComponent<RectTransform>();

            RectTransform rectTransform = popupText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = CalcCanvasPosition(position);
            rectTransform.localScale = Vector3.one * 0.7f;
            var pos =  CalcCanvasPosition(position + force);
            rectTransform.DOMove(pos, 0.4f).SetEase(Ease.OutBack);
            rectTransform.DOScale(Vector3.one * 1.5f, 0.4f).SetEase(Ease.OutBack);

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

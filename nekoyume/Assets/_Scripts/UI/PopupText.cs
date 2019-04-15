using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class PopupText : HudWidget
    {
        private static RectTransform CanvasRect;

        public Text Label;

        private Vector3 _force;
        private Vector3 _addForce;

        public static PopupText Show(Vector3 position, Vector3 force, string text)
        {
            return Show(position, force, text, Color.white);
        }

        public static PopupText Show(Vector3 position, Vector3 force, string text, Color color)
        {
            var popupText = Create<PopupText>(true);
            popupText.Label.text = text;
            popupText.Label.color = color;

            if (CanvasRect == null)
                CanvasRect = popupText.transform.root.gameObject.GetComponent<RectTransform>();

            RectTransform rectTransform = popupText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = CalcCanvasPosition(position);
            var pos =  CalcCanvasPosition(position + force);
            rectTransform.DOJumpAnchorPos(pos, 30.0f, 1, 1.0f).SetEase(Ease.OutCirc);
            popupText.Label.DOFade(0.0f, 2.0f).SetEase(Ease.InCubic);

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

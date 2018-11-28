using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class PopupText : HUD
    {
        private static RectTransform CanvasRect;

        public Text Label;

        private float _updateTime;
        private Vector3 _force;
        private Vector3 _addForce;

        public static PopupText Show(Vector3 position, Vector3 force, string text)
        {
            return Show(position, force, text, Color.white, Vector3.zero);
        }

        public static PopupText Show(Vector3 position, Vector3 force, string text, Color color, Vector3 addForce)
        {
            var popupText = UI.Widget.Create<UI.PopupText>(true);
            popupText.Label.text = text;
            popupText.Label.color = color;

            float screenHeight = Screen.height * 0.5f;
            if (CanvasRect == null)
                CanvasRect = popupText.transform.root.gameObject.GetComponent<RectTransform>();
            Vector2 viewportPosition = Camera.main.WorldToViewportPoint(position);
            Vector2 canvasPosition = new Vector2(
                ((viewportPosition.x * CanvasRect.sizeDelta.x)),
                ((viewportPosition.y * CanvasRect.sizeDelta.y)));
            RectTransform rectTransform = popupText.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = canvasPosition;

            popupText._updateTime = 0.0f;
            popupText._force = force;
            popupText._addForce = addForce;

            Destroy(popupText.gameObject, 0.8f);

            return popupText;
        }

        private void Update()
        {
            _force += _addForce;
            var pos = transform.position;
            pos += _force;
            transform.position = pos;
        }
    }
}

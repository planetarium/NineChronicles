using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Nekoyume.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PopupText : HudWidget
    {
        public Text label;

        private RectTransform _rectTransform;

        private Sequence _sequence;
        private TweenerCore<Color, Color, ColorOptions> _tweenColor;

        public static PopupText Show(Vector3 position, Vector3 force, string text)
        {
            return Show(position, force, text, Color.white);
        }

        public static PopupText Show(Vector3 position, Vector3 force, string text, Color color)
        {
            var popupText = Create<PopupText>(true);
            popupText.label.text = text;
            popupText.label.color = color;
            popupText.Show(position, force);

            return popupText;
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            _tweenColor?.Kill();
            _tweenColor = null;
            base.OnDisable();
        }

        #endregion

        private void Show(Vector3 position, Vector3 force)
        {
            var pos = position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            var tweenPos = (position + force).ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
            
            _rectTransform.anchoredPosition = pos;
            _sequence = _rectTransform.DOJumpAnchorPos(tweenPos, 30.0f, 1, 1.0f).SetEase(Ease.OutCirc);
            _tweenColor = label.DOFade(0.0f, 2.0f).SetEase(Ease.InCubic);
            
            Destroy(gameObject, 0.8f);
        }
    }
}

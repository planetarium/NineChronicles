using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(Graphic))]
    public class GraphicAlphaTweener : MonoBehaviour
    {
        [SerializeField] private float beginValue = 0f;
        [SerializeField] private float endValue = 1f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool infiniteLoop = false;

        [SerializeField] private Ease ease = Ease.Linear;
        [SerializeField] private LoopType loopType = LoopType.Yoyo;

        private Graphic _graphic;
        private TweenerCore<Color, Color, ColorOptions> _tween;
        private Color _originColor;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _originColor = _graphic.color;
        }

        private void OnEnable()
        {
            var color = new Color(_originColor.r, _originColor.g, _originColor.b, beginValue);
            _graphic.color = color;

            color.a = endValue;
            _tween = _graphic.DOColor(color, duration)
                .SetEase(ease);

            if (infiniteLoop)
                _tween = _tween.SetLoops(-1, loopType);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _graphic.color = _originColor;
        }
    }
}

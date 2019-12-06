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
        private Color _color;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            _color = _graphic.color;
            _color.a = beginValue;
            _tween = _graphic.DOColor(_color, 0.0f);

            _color.a = endValue;
            _tween = _graphic.DOColor(_color, duration)
                .SetEase(ease);

            if (infiniteLoop)
                _tween = _tween.SetLoops(-1, loopType);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _color.a = beginValue;
            _graphic.color = _color;
        }
    }
}

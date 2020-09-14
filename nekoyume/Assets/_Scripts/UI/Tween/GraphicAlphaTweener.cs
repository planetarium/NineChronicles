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
        [SerializeField]
        private float beginValue = 0f;

        [SerializeField]
        private float endValue = 1f;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private bool infiniteLoop = false;

        [SerializeField]
        private Ease ease = Ease.Linear;

        [SerializeField]
        private LoopType loopType = LoopType.Yoyo;

        private Graphic _graphic;
        private Color _originColor;

        protected Tweener Tweener { get; set; }

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
            Tweener = _graphic.DOColor(color, duration)
                .SetEase(ease);

            if (infiniteLoop)
            {
                Tweener = Tweener.SetLoops(-1, loopType);
            }
        }

        private void OnDisable()
        {
            KillTween();
            _graphic.color = _originColor;
        }

        public void KillTween()
        {
            if (Tweener?.IsPlaying() ?? false)
            {
                Tweener?.Kill();
            }

            Tweener = null;
        }
    }
}

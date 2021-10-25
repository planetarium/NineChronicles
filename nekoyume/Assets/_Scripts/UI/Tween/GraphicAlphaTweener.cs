using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(Graphic))]
    public class GraphicAlphaTweener : DOTweenBase
    {
        [SerializeField]
        private float beginValue = 0f;

        [SerializeField]
        private float endValue = 1f;

        [SerializeField]
        private bool infiniteLoop = false;

        [SerializeField]
        private LoopType loopType = LoopType.Yoyo;

        private Graphic _graphic;
        private Color _originColor;

        protected override void Awake()
        {
            base.Awake();
            _graphic = GetComponent<Graphic>();
            _originColor = _graphic.color;
        }

        private void OnEnable()
        {
            _graphic.DOFade(beginValue, 0f);
            currentTween = _graphic.DOFade(endValue, duration).SetEase(ease);

            if (infiniteLoop)
            {
                currentTween = currentTween.SetLoops(-1, loopType);
            }
        }

        private void OnDisable()
        {
            Stop();
            _graphic.color = _originColor;
        }
    }
}

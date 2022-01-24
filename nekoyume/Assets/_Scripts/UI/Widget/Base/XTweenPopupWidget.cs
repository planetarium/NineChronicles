using DG.Tweening;
using Nekoyume.UI.Tween;
using System.Collections;
using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class XTweenPopupWidget : PopupWidget
    {
        [SerializeField]
        private AnchoredPositionSingleTweener xTweener = null;

        protected override void Awake()
        {
            base.Awake();
            xTweener.axisType = AnchoredPositionSingleTweener.AxisType.X;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            xTweener
                .PlayTween()
                .OnComplete(OnTweenComplete);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            StartCoroutine(CoClose(ignoreCloseAnimation));
        }

        private IEnumerator CoClose(bool ignoreCloseAnimation)
        {
            xTweener.PlayReverse().OnComplete(OnTweenReverseComplete);
            var playing = true;
            while (playing)
            {
                playing = xTweener != null && xTweener.IsPlaying;
                yield return null;
            }

            base.Close(ignoreCloseAnimation);
        }

        protected virtual void OnTweenComplete()
        {
        }

        protected virtual void OnTweenReverseComplete()
        {
        }
    }
}

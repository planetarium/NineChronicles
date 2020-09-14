using DG.Tweening;
using Nekoyume.UI.Tween;
using System.Collections;
using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class XTweenWidget : Widget
    {
        [SerializeField]
        private AnchoredPositionXTweener xTweener = null;

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
            yield return new WaitWhile(xTweener
                .PlayReverse()
                .OnComplete(OnTweenReverseComplete)
                .IsPlaying);
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

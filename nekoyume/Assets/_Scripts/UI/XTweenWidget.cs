using DG.Tweening;
using Nekoyume.UI.Tween;
using System.Collections;
using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class XTweenWidget:Widget
    {
        protected AnchoredPositionXTweener _xTween;

        protected override void Awake()
        {
            base.Awake();
            _xTween = GetComponentInChildren<AnchoredPositionXTweener>();
        }

        public override void Show()
        { 
            base.Show();
            _xTween.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (!gameObject.activeSelf)
                return;

            StartCoroutine(CoClose(ignoreCloseAnimation));
        }

        private IEnumerator CoClose(bool ignoreCloseAnimation)
        {
            yield return new WaitWhile(_xTween.Close().IsPlaying);
            base.Close(ignoreCloseAnimation);
        }
    }
}
using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public abstract class BaseTweener : MonoBehaviour
    {
        protected Tweener Tweener { get; set; }

        public bool IsPlaying => Tweener != null && Tweener.IsPlaying();

        public bool IsActive => Tweener != null && Tweener.IsActive();

        public abstract Tweener PlayTween();

        public abstract Tweener PlayReverse();

        public abstract void ResetToOrigin();

        public void KillTween()
        {
            if (Tweener != null && Tweener.IsActive() && Tweener.IsPlaying())
            {
                Tweener.Kill();
                Tweener = null;
            }
        }
    }
}

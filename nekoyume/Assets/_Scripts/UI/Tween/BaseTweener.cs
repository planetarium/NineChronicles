using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public abstract class BaseTweener : MonoBehaviour
    {
        protected Tweener Tweener { get; set; }

        public bool IsPlaying => Tweener?.IsPlaying() ?? false;

        public abstract Tweener PlayTween();

        public abstract Tweener PlayReverse();

        public abstract void ResetToOrigin();

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

using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public abstract class BaseTweener : MonoBehaviour
    {
        protected Tweener Tweener { get; set; }

        public abstract Tweener PlayTween();

        public abstract Tweener PlayReverse();

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

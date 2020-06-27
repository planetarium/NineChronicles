using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public class BaseTweener : MonoBehaviour
    {
        protected Tweener Tweener { get; set; }

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

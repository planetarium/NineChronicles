using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Nekoyume.UI
{
    public abstract class DialogEffect
    {
        public abstract void Execute(DialogPopup widget);
    }

    public class DialogEffectShake : DialogEffect
    {
        public Vector3 value = new Vector3();
        public float duration = 1.0f;
        public int loops = 1;

        public override void Execute(DialogPopup widget)
        {
            duration = duration / (loops * 2.0f);
            var seq = DOTween.Sequence();
            for (int i = 0; i < loops; ++i)
            {
                seq.Append(widget.imgCharacter.transform.DOBlendableLocalMoveBy(value, duration));
                seq.Append(widget.imgCharacter.transform.DOBlendableLocalMoveBy(-value, duration));
            }
            seq.Play();
        }
    }
}

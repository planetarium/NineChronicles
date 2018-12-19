using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.CC
{
    public interface IKnockBack : ICCBase
    {
        float Power { get; }
    }

    public class KnockBack : CCBase, IKnockBack
    {
        public float Power { get; private set; }

        public new void Set(float power, float duration = 0.5f)
        {
            Power = power;
            base.Set(duration);
        }

        protected override void OnBegin()
        {
            Owner.CancelCast();
            Vector3 endPosition = transform.TransformPoint(Power, 0.0f, 0.0f);
            transform.DOJump(endPosition, 0.02f, 1, Duration);
        }
    }
}

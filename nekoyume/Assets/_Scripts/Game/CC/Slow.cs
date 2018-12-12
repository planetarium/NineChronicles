using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.CC
{
    public interface ISlow : ICCBase
    {
        float AttackSpeedMultiplier { get; }
        float WalkSpeedMultiplier { get; }
    }

    public class Slow : CCBase, ISlow
    {
        public float AttackSpeedMultiplier { get; private set; }
        public float WalkSpeedMultiplier { get; private set; }

        public void Set(float attackSpeedMultiplier, float walkSpeedMultiplier, float duration)
        {
            AttackSpeedMultiplier = attackSpeedMultiplier;
            WalkSpeedMultiplier = walkSpeedMultiplier;
            base.Set(duration);
        }

        protected override void OnTickBefore()
        {
            PopupText.Show(
                transform.TransformPoint(-0.5f, Random.Range(0.0f, 0.5f), 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                "Slowed!",
                Color.gray
            );
        }
    }
}

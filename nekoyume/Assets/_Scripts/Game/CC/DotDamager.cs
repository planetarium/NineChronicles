using System.Collections;
using Nekoyume.Data.Table;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.CC
{
    public interface IDotDamager : ICCBase
    {
        AttackType AttackType { get; }
        int DamagePerTick { get; }
    }

    public class DotDamager : CCBase, IDotDamager
    {
        public AttackType AttackType { get; private set; }
        public int DamagePerTick { get; private set; }

        public void Set(AttackType attackType, int damagePerTick, float duration, float tick = 1.0f)
        {
            AttackType = attackType;
            DamagePerTick = damagePerTick;
            base.Set(duration, tick);
        }

        protected override void OnTickBefore()
        {
            OnDamage();
            PopupText.Show(
                transform.TransformPoint(-0.5f, Random.Range(0.0f, 0.5f), 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                "Poisoned!",
                Color.magenta
            );
        }

        protected virtual void OnDamage()
        {
            Owner.OnDamage(AttackType, DamagePerTick, false);
        }
    }
}

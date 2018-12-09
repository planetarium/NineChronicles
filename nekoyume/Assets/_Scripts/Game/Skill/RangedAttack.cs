namespace Nekoyume.Game.Skill
{
    public class RangedAttack : Attack
    {
        protected override bool _Use()
        {
            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Bullet>();
            Damager(damager, 0.0f, "hit_02");

            return true;
        }

    }
}

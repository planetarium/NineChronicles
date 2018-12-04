namespace Nekoyume.Game.Skill
{
    public class RangedAttack : Attack
    {
        public override bool Use()
        {
            if (IsCooltime())
                return false;

            _cooltime = (float)_data.Cooltime;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Bullet>();
            Damager(damager, 0.0f, "hit_02");

            return true;
        }

    }
}

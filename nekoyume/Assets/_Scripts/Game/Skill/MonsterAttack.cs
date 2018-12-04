namespace Nekoyume.Game.Skill
{
    public class MonsterAttack : SkillBase
    {
        private void Awake()
        {
            _targetTag = Tag.Player;
        }

        override public bool Use()
        {
            if (IsCooltime())
                return false;

            _cooltime = (float)_data.Cooltime;

            float range = (float)_data.Range / (float)Game.PixelPerUnit;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Damager>();
            Damager(damager, -range, "hit_04");

            return true;
        }
    }
}

namespace Nekoyume.Game.Skill
{
    public class Attack : SkillBase
    {
        private void Awake()
        {
            _targetTag = Tag.Enemy;
        }

        public override bool Use()
        {
            if (IsCooltime())
                return false;

            _cooltime = (float)_data.Cooltime;

            float range = (float)_data.Range / (float)Game.PixelPerUnit;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Damager>();
            Damager(damager, range, "hit_01");

            return true;
        }
    }
}

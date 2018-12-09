namespace Nekoyume.Game.Skill
{
    public class Attack : SkillBase
    {
        private void Awake()
        {
            _targetTag = Tag.Enemy;
        }

        protected override bool _Use()
        {
            _knockBack = 0.2f;

            float range = (float)_data.Range / (float)Game.PixelPerUnit;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Damager>();
            Damager(damager, range, "hit_01");

            return true;
        }
    }
}

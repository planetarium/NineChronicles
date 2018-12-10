namespace Nekoyume.Game.Skill
{
    public class MonsterAttack : SkillBase
    {
        private void Awake()
        {
            _targetTag = Tag.Player;
        }

        protected override bool _Use()
        {
            float range = (float)Data.Range / (float)Game.PixelPerUnit;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Damager>();
            Damager(damager, -range, "hit_04");

            return true;
        }
    }
}

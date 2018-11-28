using UnityEngine;


namespace Nekoyume.Game.Skill
{
    public class Attack : SkillBase
    {
        private void Awake()
        {
            _targetTag = Tag.Enemy;
        }

        override public bool Use()
        {
            if (IsCooltime())
                return false;

            _cooltime = (float)_data.Cooltime;

            Character.CharacterBase owner = GetComponent<Character.CharacterBase>();
            int damage = Mathf.FloorToInt(owner.CalcAtk() * ((float)_data.Power * 0.01f));
            float range = (float)_data.Range / (float)Game.PixelPerUnit;
            float size = (float)_data.Size / (float)Game.PixelPerUnit;

            var objectPool = GetComponentInParent<Util.ObjectPool>();
            var damager = objectPool.Get<Trigger.Damager>(transform.TransformPoint(range, 0.0f, 0.0f));
            damager.Set("hit_01", _targetTag, _data.AttackType, damage, size, _data.TargetCount, 0.2f);

            return true;
        }
    }
}

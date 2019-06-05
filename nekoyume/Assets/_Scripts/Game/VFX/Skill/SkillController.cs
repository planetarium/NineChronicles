using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillController : MonoBehaviour
    {
        public SkillVFX[] skills;
        private ObjectPool _pool;

        private void Awake()
        {
            _pool = FindObjectOfType<ObjectPool>();
            skills = Resources.LoadAll<SkillVFX>("VFX/Prefabs");
            foreach (var skill in skills)
            {
                _pool.Add(skill.gameObject, 5);
            }
        }

        public T Get<T>(CharacterBase target, Model.Skill.SkillInfo skillInfo) where T : SkillVFX
        {
            var position = target.transform.position;
            position.x -= 0.2f;
            position.y += 0.32f;
            var size = target.characterSize == "xs" ? "s" : "m";
            var elemental = skillInfo.Elemental;
            if (skillInfo.Category == SkillEffect.Category.Area)
            {
                size = "l";
                var pos = ActionCamera.instance.Cam.ScreenToWorldPoint(
                    new Vector2((float) Screen.width / 2, 0));
                position.x = pos.x + 0.5f;
                elemental = Data.Table.Elemental.ElementalType.Fire;
            }
            var skillName = $"{skillInfo.Category}_{size}_{elemental}".ToLower();
            var go = _pool.Get(skillName, false, position);
            var effect = go.GetComponent<T>();
            effect.target = target;
            effect.Stop();
            return effect;
        }

        public SkillCastingVFX Get(Vector3 position, Model.Skill.SkillInfo skillInfo)
        {
            //TODO 속성별 캐스팅 마법진이 달라야함.
            var elemental = Data.Table.Elemental.ElementalType.Normal;
            if (skillInfo.Elemental == Data.Table.Elemental.ElementalType.Fire)
                elemental = Data.Table.Elemental.ElementalType.Fire;
            var skillName = $"casting_{elemental}".ToLower();
            var go = _pool.Get(skillName, false, position);
            var effect = go.GetComponent<SkillCastingVFX>();
            effect.Stop();
            return effect;
        }
    }
}

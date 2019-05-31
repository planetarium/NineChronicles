using Nekoyume.Data.Table;
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

        public T Get<T>(string size, Model.Skill.SkillInfo skillInfo, Vector3 position) where T : SkillVFX
        {
            if (skillInfo.Category == SkillEffect.Category.Area)
                size = "l";
            else
                size = size == "xs" ? "s" : "m";
            var skillName = $"{skillInfo.Category}_{size}_{skillInfo.Elemental}".ToLower();
            var go = _pool.Get(skillName, false, position);
            return go.GetComponent<T>();
        }

        public SkillCastingVFX Get(Vector3 position)
        {
            //TODO 속성별 캐스팅 마법진이 달라야함.
            var go = _pool.Get("casting_fire", false, position);
            return go.GetComponent<SkillCastingVFX>();
        }

    }
}

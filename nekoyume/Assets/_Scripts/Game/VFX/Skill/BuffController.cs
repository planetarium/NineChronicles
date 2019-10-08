using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffController : MonoBehaviour
    {
        private const int InitCount = 5;

        public BuffVFX[] buffs;
        private ObjectPool _pool;

        private void Awake()
        {
            _pool = FindObjectOfType<ObjectPool>();
            buffs = Resources.LoadAll<BuffVFX>("VFX/Prefabs");
            foreach (var buff in buffs)
            {
                _pool.Add(buff.gameObject, InitCount);
            }
        }

        public T Get<T>(CharacterBase target, Model.Skill.SkillInfo skillInfo) where T : BuffVFX
        {
            var position = target.transform.position;
            position.x -= 0.2f;
            position.y += 0.32f;
            var skillName = "buff_plus_attack";
            var go = _pool.Get(skillName, false, position);
            if (go == null)
            {
                go = _pool.Get(skillName, true, position);
            }
            var effect = go.GetComponent<T>();
            if (effect == null)
            {
                Debug.LogError(skillName);
            }
            effect.target = target;
            effect.Stop();
            return effect;
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff)
        {
            var buffName = "buff_plus_casting";
            var go = _pool.Get(buffName, false, position);
            var effect = go.GetComponent<BuffCastingVFX>();
            effect.Stop();
            return effect;
        }

    }
}

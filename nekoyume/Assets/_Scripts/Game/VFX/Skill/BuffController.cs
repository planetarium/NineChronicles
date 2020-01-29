using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using Nekoyume.Model.Buff;
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

        public T Get<T>(CharacterBase target, Buff buff) where T : BuffVFX
        {
            var position = target.transform.position;
            position.y += 0.55f;
            var resource = buff.RowData.IconResource;
            var resourceName = resource.Replace("icon_", "");
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }
            var effect = go.GetComponent<T>();
            if (effect == null)
            {
                Debug.LogError(resourceName);
            }
            effect.target = target;
            effect.Stop();
            return effect;
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff)
        {
            string buffName;
            if (buff is HPBuff)
            {
                buffName = "buff_hp_casting";
            }
            else
            {
                buffName = buff.RowData.StatModifier.Value > 0 ? "buff_plus_casting" : "buff_minus_casting";
            }

            position.y += 0.55f;
            var go = _pool.Get(buffName, false, position);
            var effect = go.GetComponent<BuffCastingVFX>();
            effect.Stop();
            return effect;
        }

    }
}

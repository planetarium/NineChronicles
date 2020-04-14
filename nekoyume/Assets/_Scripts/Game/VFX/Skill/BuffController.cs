using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using Nekoyume.Model.Buff;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffController
    {
        private const int InitCount = 5;

        private readonly ObjectPool _pool;

        public BuffController(ObjectPool objectPool)
        {
            _pool = objectPool;
            var buffs = Resources.LoadAll<BuffVFX>("VFX/Prefabs");
            foreach (var buff in buffs)
            {
                _pool.Add(buff.gameObject, InitCount);
            }
        }

        public T Get<T>(CharacterBase target, Buff buff) where T : BuffVFX
        {
            if (target is null)
            {
                return null;
            }

            var position = target.transform.position;
            position.y += 0.55f;
            var resource = buff.RowData.IconResource;
            var resourceName = resource.Replace("icon_", "");
            var go = _pool.Get(resourceName, false, position) ??
                     _pool.Get(resourceName, true, position);

            return GetEffect<T>(go, target);
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
                buffName = buff.RowData.StatModifier.Value > 0
                    ? "buff_plus_casting"
                    : "buff_minus_casting";
            }

            position.y += 0.55f;
            var go = _pool.Get(buffName, false, position) ??
                     _pool.Get(buffName, true, position);

            return GetEffect<BuffCastingVFX>(go);
        }

        private static T GetEffect<T>(GameObject go, CharacterBase target = null)
            where T : BuffVFX
        {
            var effect = go.GetComponent<T>();
            if (effect is null)
            {
                throw new NotFoundComponentException<T>(go.name);
            }

            if (!(target is null))
            {
                effect.target = target;
            }

            effect.Stop();
            return effect;
        }
    }
}

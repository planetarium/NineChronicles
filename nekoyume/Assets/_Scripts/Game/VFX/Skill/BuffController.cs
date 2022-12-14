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

        public V Get<T, V>(T target, Buff buff)
            where T : Character.Character
            where V : BuffVFX
        {
            if (target is null)
            {
                return null;
            }

            var position = target.transform.position;
            position.y += 0.55f;

            string resourceName = string.Empty;
            if (buff is StatBuff statBuff)
            {
                var resource = statBuff.RowData.IconResource;
                resourceName = resource.Replace("icon_", "");
            }
            else if (buff is ActionBuff actionBuff)
            {
                resourceName = $"actionBuff_{actionBuff.RowData.ActionBuffType}";
            }

            var go = _pool.Get(resourceName, false, position) ??
                     _pool.Get(resourceName, true, position);

            return GetEffect<V>(go);
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff)
        {
            string buffName = string.Empty;
            if (buff is HPBuff)
            {
                buffName = "buff_hp_casting";
            }
            else if (buff is DamageReductionBuff)
            {
                // TODO : Will be removed when damage reduction buff is attached on equipments
                buffName = "buff_staking_casting";
            }
            else if (buff is StatBuff statBuff)
            {
                buffName = statBuff.RowData.StatModifier.Value >= 0
                    ? "buff_plus_casting"
                    : "buff_minus_casting";
            }
            else if (buff is ActionBuff actionBuff)
            {
                buffName = $"{actionBuff.RowData.ActionBuffType}_casting";
            }

            position.y += 0.55f;
            var go = _pool.Get(buffName, false, position) ??
                     _pool.Get(buffName, true, position);

            return GetEffect<BuffCastingVFX>(go);
        }

        private static T GetEffect<T>(GameObject go)
            where T : BuffVFX
        {
            var effect = go.GetComponent<T>();
            if (effect is null)
            {
                throw new NotFoundComponentException<T>(go.name);
            }

            effect.Stop();
            return effect;
        }

        public T Get<T>(GameObject target, Buff buff) where T : BuffVFX
        {
            var position = target.transform.position;
            position.y += 0.55f;

            var resourceName = string.Empty;
            if (buff is StatBuff statBuff)
            {
                var resource = statBuff.RowData.IconResource;
                resourceName = resource.Replace("icon_", "");
            }
            else if (buff is ActionBuff actionBuff)
            {
                resourceName = $"actionBuff_{actionBuff.RowData.ActionBuffType}";
            }
            var go = _pool.Get(resourceName, false, position) ??
                     _pool.Get(resourceName, true, position);

            return GetEffect<T>(go);
        }
    }
}

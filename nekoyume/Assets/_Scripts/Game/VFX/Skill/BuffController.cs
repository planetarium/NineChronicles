using JetBrains.Annotations;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using Nekoyume.Helper;
using Nekoyume.Model.Buff;
using System.Collections;
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

            var resourceName = BuffHelper.GetBuffVFXPrefab(buff).name;
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }

            return GetEffect<V>(go);
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff)
        {
            var resourceName = BuffHelper.GetCastingVFXPrefab(buff).name;
            position.y += 0.55f;
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }

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


            var resourceName = BuffHelper.GetBuffVFXPrefab(buff).name;
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }

            return GetEffect<T>(go);
        }

        public static IEnumerator CoChaseTarget(Component vfx, Transform target)
        {
            var g = vfx.gameObject;
            var t = vfx.transform;
            while (g.activeSelf &&
                   target)
            {
                t.position = target.position + new Vector3(0f, 0.55f, 0f);
                yield return null;
            }
        }
    }
}

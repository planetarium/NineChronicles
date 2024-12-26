using Nekoyume.Game.Util;
using Nekoyume.Helper;
using Nekoyume.Model.Buff;
using System.Collections;
using Cysharp.Threading.Tasks;
using Nekoyume.Model.Skill;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffController
    {
#if UNITY_ANDROID || UNITY_IOS
        private const int InitCount = 1;
#else
        private const int InitCount = 5;
#endif

        private ObjectPool _pool;

        public async UniTask InitializeAsync(ObjectPool objectPool)
        {
            _pool = objectPool;
            await ResourceManager.Instance.LoadAllAsync<GameObject>(ResourceManager.BuffLabel, true, assetAddress =>
            {
                var prefab = ResourceManager.Instance.Load<GameObject>(assetAddress);
                if (prefab == null)
                {
                    NcDebug.LogError($"Failed to load {assetAddress}");
                    return;
                }
                _pool.Add(prefab, InitCount);
            });
        }

        public T Get<T>(GameObject target, Buff buff, TableSheets tableSheets) where T : BuffVFX
        {
            if (target == null)
            {
                return null;
            }

            var position = target.transform.position + BuffHelper.GetBuffPosition(buff.BuffInfo.Id, tableSheets);

            var resourceName = BuffHelper.GetBuffVFXPrefab(buff, tableSheets).name;
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }

            return GetEffect<T>(go);
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff, TableSheets tableSheets)
        {
            // TODO: ID대신 GroupID사용 고려 혹은 ID와 GroupID사이의 정의 정리
            var resourceName = BuffHelper.GetCastingVFXPrefab(buff, tableSheets).name;
            position += BuffHelper.GetBuffPosition(buff.BuffInfo.Id, tableSheets, true);
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
            if (!go.TryGetComponent<T>(out var effect))
            {
                return null;
            }

            effect.Stop();
            return effect;
        }

        public static IEnumerator CoChaseTarget(Component vfx, Character.Character target, Buff buffModel, TableSheets tableSheets)
        {
            var g = vfx.gameObject;
            var t = vfx.transform;
            while (g.activeSelf && target)
            {
                t.position = target.transform.position + BuffHelper.GetBuffPosition(buffModel.BuffInfo.Id, tableSheets);

                if (buffModel is ActionBuff actionBuff)
                {
                    if (actionBuff.RowData.ActionBuffType == ActionBuffType.IceShield)
                    {
                        vfx.transform.FlipX(target.IsFlipped);
                    }
                }

                yield return null;
            }
        }
    }
}

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

        private readonly ObjectPool _pool;

        public BuffController(ObjectPool objectPool)
        {
            _pool = objectPool;
        }
        
        public async UniTask InitializeAsync()
        {
            // TODO: BuffVfx 상속받은 프리팹만 로드하도록 에셋 구분
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

        public T Get<T>(GameObject target, Buff buff) where T : BuffVFX
        {
            if (target == null)
            {
                return null;
            }

            var position = target.transform.position + BuffHelper.GetBuffPosition(buff.BuffInfo.Id);

            var resourceName = BuffHelper.GetBuffVFXPrefab(buff).name;
            var go = _pool.Get(resourceName, false, position);
            if (go == null)
            {
                go = _pool.Get(resourceName, true, position);
            }

            return GetEffect<T>(go);
        }

        public BuffCastingVFX Get(Vector3 position, Buff buff)
        {
            // TODO: ID대신 GroupID사용 고려 혹은 ID와 GroupID사이의 정의 정리
            var resourceName = BuffHelper.GetCastingVFXPrefab(buff).name;
            position += BuffHelper.GetBuffPosition(buff.BuffInfo.Id, true);
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

        public static IEnumerator CoChaseTarget(Component vfx, Character.Character target, Buff buffModel)
        {
            var g = vfx.gameObject;
            var t = vfx.transform;
            while (g.activeSelf && target)
            {
                t.position = target.transform.position + BuffHelper.GetBuffPosition(buffModel.BuffInfo.Id);

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

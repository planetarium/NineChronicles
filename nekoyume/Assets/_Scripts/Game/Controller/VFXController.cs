using System.Collections;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Controller
{
    public class VFXController : MonoSingleton<VFXController>
    {
        protected override bool ShouldRename => true;

        private ObjectPool _pool = null;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _pool = FindObjectOfType<ObjectPool>();
            if (ReferenceEquals(_pool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }
        }

        #endregion

        public T Create<T>(Vector3 position) where T : VFX.VFX
        {
            var vfx = _pool.Get<T>(position);
            return vfx;
        }

        public T Create<T>(Transform target, Vector3 offset) where T : VFX.VFX
        {
            var vfx = _pool.Get<T>();
            StartCoroutine(CoChaseTarget(vfx, target, offset));
            return vfx;
        }

        private static IEnumerator CoChaseTarget(Component vfx, Transform target, Vector3 offset)
        {
            var g = vfx.gameObject;
            var t = vfx.transform;
            while (g.activeSelf &&
                   target)
            {
                t.position = target.position + offset;

                yield return null;
            }
        }
    }
}

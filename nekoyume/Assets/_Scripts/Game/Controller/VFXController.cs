using System.Collections;
using Nekoyume.Game.Util;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Game.Controller
{
    [DefaultExecutionOrder(350)]
    public class VFXController : MonoSingleton<VFXController>
    {
        protected override bool ShouldRename => true;

        private ObjectPool _pool = null;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _pool = Game.instance.Stage.objectPool;
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

        public T CreateAndChase<T>(Transform target, Vector3 offset) where T : VFX.VFX
        {
            var vfx = _pool.Get<T>(target.position + offset);
            StartCoroutine(CoChaseTarget(vfx, target, offset));
            return vfx;
        }

        public T CreateAndChaseCam<T>(Vector3 position) where T : VFX.VFX
        {
            var target = ActionCamera.instance.transform;
            var targetPosition = target.position;
            var offset = position - targetPosition;
            offset.z += 10f;
            var vfx = _pool.Get<T>(targetPosition + offset);
            StartCoroutine(CoChaseTarget(vfx, target, offset));
            return vfx;
        }

        public T CreateAndChaseCam<T>(Vector3 position, Vector3 offset) where T : VFX.VFX
        {
            return CreateAndChaseCam<T>(position + offset);
        }

        // FIXME: RectTransform이 아니라 Transform을 받아도 되겠습니다.
        public T CreateAndChaseRectTransform<T>(RectTransform target) where T : VFX.VFX
        {
            return CreateAndChaseRectTransform<T>(target, target.position);
        }

        // FIXME: RectTransform이 아니라 Transform을 받아도 되겠습니다.
        public T CreateAndChaseRectTransform<T>(RectTransform target, Vector3 position) where T : VFX.VFX
        {
            var targetPosition = target.position;
            var offset = position - targetPosition;
            offset.z += 10f;
            var vfx = _pool.Get<T>(targetPosition + offset);
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

using Nekoyume.Game.Util;
using Nekoyume.Game.Vfx;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class VfxFactory : MonoSingleton<VfxFactory>
    {
        private ObjectPool _pool = null;

        protected override void Awake()
        {
            base.Awake();

            _pool = FindObjectOfType<ObjectPool>();
            if (ReferenceEquals(_pool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }
        }

        public T Create<T>(Vector3 position) where T : VfxBase
        {
            var vfx = _pool.Get<T>(position);
            return vfx;
        }
    }
}

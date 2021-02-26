using System;
using UniRx.Toolkit;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Nekoyume.Pool
{
    [Serializable]
    public class SimpleUIImagePool : ObjectPool<Image>
    {
        [SerializeField]
        private RectTransform parent = null;

        [SerializeField]
        private Image prefab = null;

        protected override Image CreateInstance()
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}

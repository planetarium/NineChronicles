using System;
using TMPro;
using UniRx.Toolkit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nekoyume.Pool
{
    [Serializable]
    public class SimpleTextMeshProUGUIPool : ObjectPool<TextMeshProUGUI>
    {
        [SerializeField]
        private RectTransform parent = null;

        [SerializeField]
        private TextMeshProUGUI prefab = null;

        protected override TextMeshProUGUI CreateInstance()
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}

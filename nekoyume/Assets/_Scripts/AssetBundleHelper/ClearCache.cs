using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    public class ClearCache : MonoBehaviour
    {
        private void Start()
        {
            UnityEngine.Caching.ClearCache();
        }
    }
}

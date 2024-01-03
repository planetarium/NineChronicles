using System;
using System.Collections;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    public class AssetBundleDownloadTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            foreach (var bundleName in AssetBundleData.AssetBundleNames)
            {
                yield return AssetBundleLoader.DownloadAssetBundles(
                    bundleName,
                    progress => { Debug.Log($"{bundleName} - {progress * 100}%"); });
            }

            Instantiate(
                AssetBundleLoader.LoadAssetBundle<GameObject>("vfx/skills", "areaattack_l_fire"));

            Instantiate(
                AssetBundleLoader.LoadAssetBundle<GameObject>("vfx/skills", "areaattack_l_water"));
        }
    }
}

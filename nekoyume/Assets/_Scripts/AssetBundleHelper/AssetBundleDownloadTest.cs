using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            SceneManager.LoadScene(1);
        }
    }
}

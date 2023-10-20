using System.Collections;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    public class AssetBundleDownloadTest : MonoBehaviour
    {
        [SerializeField] private AssetBundleSettings assetBundleSettings;

        private IEnumerator Start()
        {
            foreach (var bundleName in assetBundleSettings.AssetBundleNames)
            {
                yield return AssetBundleLoader.DownloadAssetBundles(
                    assetBundleSettings.AssetBundleURL, bundleName,
                    progress => { Debug.Log($"{bundleName} - {progress * 100}%"); });
            }

            /*
            print("done!");
            yield return AssetBundleLoader.LoadAssetBundleAsync<GameObject>("vfx/skills", "areaattack_l_fire", obj =>
            {
                Instantiate(obj);
            });

            Instantiate(AssetBundleLoader.LoadAssetBundle<GameObject>("vfx/skills", "areaattack_l_water"));
            */
        }
    }
}

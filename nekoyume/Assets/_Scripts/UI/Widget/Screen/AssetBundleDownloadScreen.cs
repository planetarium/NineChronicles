using System;
using System.Collections;
using Nekoyume.AssetBundleHelper;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nekoyume.UI
{
    public class AssetBundleDownloadScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text progressText;

        private void Start()
        {
            StartCoroutine(DownloadAssetBundle(() =>
            {
                SceneManager.LoadScene("Game");
            }));
        }

        private IEnumerator DownloadAssetBundle(System.Action then)
        {
            var settings = Resources.Load<AssetBundleSettings>("AssetBundleSettings");
            foreach (var bundleName in settings.AssetBundleNames)
            {
                yield return AssetBundleLoader.DownloadAssetBundles(
                    settings.AssetBundleURL, bundleName,
                    progress => { progressText.text = $"{bundleName} - {progress * 100}%"; });
            }
            then();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Networking;
using Nekoyume.Game.VFX.Skill;

namespace Nekoyume.AddressablesHelper
{
    public class AddressablesDownloader : MonoBehaviour
    {
        public bool ClearCache;

        long BToKb(long bytes)
        {
            return bytes / 1024;
        }

        // Start is called before the first frame update
        private IEnumerator Start()
        {
            var allKeys = new string[]
                { "audio/music", "audio/sfx", "ui", "character", "video", "vfx" };
            if (ClearCache)
            {
                UnityEngine.Caching.ClearCache();
            }

            foreach (var key in allKeys)
            {
                var downloader = Addressables.GetDownloadSizeAsync(key);
                yield return downloader;
                var keyDownloadSizeKb = BToKb(downloader.Result);
                Debug.Log(key + " : " + keyDownloadSizeKb);
                if (keyDownloadSizeKb <= 0) continue;

                var keyDownloadOperation = Addressables.DownloadDependenciesAsync(key);
                while (!keyDownloadOperation.IsDone)
                {
                    yield return null;
                    var acquiredKb = keyDownloadOperation.PercentComplete * keyDownloadSizeKb;
                    var totalProgressPercentage = (acquiredKb / keyDownloadSizeKb);
                    Debug.Log("Download progress: " +
                              (totalProgressPercentage * 100).ToString("0.00") + "% - " +
                              acquiredKb + "kb /" + keyDownloadSizeKb + "kb");
                }
            }

            //SceneManager.LoadScene(1);

            var handler = Addressables.LoadAssetsAsync<GameObject>("vfx", (go) =>
            {
                var skillVFX = go.GetComponent<SkillVFX>();
                if (skillVFX)
                {
                    skills.Add(skillVFX);
                }
            });
            handler.WaitForCompletion();
            Debug.Log("Done");
        }

        List<SkillVFX> skills = new List<SkillVFX>();

        public bool test = false;
        public bool next = false;
        public int index = 0;

        private void Update()
        {
            if (test)
            {
                test = false;
                Instantiate(skills[index % skills.Count].gameObject);
            }

            if (next)
            {
                SceneManager.LoadScene(1);
            }
        }
    }
}

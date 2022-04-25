using System;
using System.Collections;
using System.IO;
using Nekoyume.BlockChain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Planetarium.Nekoyume.Editor
{
    public static class StoreDownloader
    {
        [MenuItem("Tools/Store/Download and Extract Main-net Store", true)]
        public static bool Validation() => !Application.isPlaying;

        [MenuItem("Tools/Store/Download and Extract Main-net Store")]
        public static void DownloadAndExtractMainNetStore()
        {
            if (!EditorUtility.DisplayDialog("Question", "This job takes a very long time. Do you want to continue?",
                "Yes", "No"))
            {
                Debug.Log("Downloading store canceled");
                return;
            }

            var selectedFolder = EditorUtility.SaveFolderPanel(
                "Select Folder",
                StorePath.GetPrefixPath(),
                "9c_dev");
            if (string.IsNullOrEmpty(selectedFolder))
            {
                Debug.Log("Downloading store canceled");
                return;
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadAndExtractMainNetStoreAsync(selectedFolder));
        }

        private static IEnumerator DownloadAndExtractMainNetStoreAsync(string extractPath)
        {
            const string url =
                "https://9c-snapshots.s3.ap-northeast-2.amazonaws.com/main/partition/full/9c-main-snapshot.zip";
            // const string url = "https://drive.google.com/uc?export=download&id=10kGM9sPSTjw0tdej2ITKzovWQxgbZubM";
            using var request = UnityWebRequest.Get(url);
            var fileName = $"{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.zip";
            var downloadFilePath = Path.Combine(
                Application.temporaryCachePath,
                fileName);
            using var downloadHandler = new DownloadHandlerFile(downloadFilePath);
            downloadHandler.removeFileOnAbort = true;
            request.downloadHandler = downloadHandler;
            var asyncOperation = request.SendWebRequest();
            while (!asyncOperation.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Download",
                    $"url: {url}\ndownload to: {downloadFilePath}",
                    asyncOperation.progress))
                {
                    request.Abort();
                    Debug.Log("Downloading store canceled");
                    EditorUtility.ClearProgressBar();
                    yield break;
                }

                yield return new WaitForSeconds(.1f);
            }

            yield return asyncOperation;
            Debug.Log($"Download completed. \"{downloadFilePath}\"");
            EditorUtility.ClearProgressBar();

            if (request.result != UnityWebRequest.Result.Success)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to download the Main-net Store at \"{url}\"", "ok");
                yield break;
            }

            if (!File.Exists(downloadFilePath))
            {
                EditorUtility.DisplayDialog("Error", $"Zip file not exist at \"{downloadFilePath}\"", "ok");
                yield break;
            }

            ZipUnzip.Unzip(downloadFilePath, extractPath);
            if (EditorUtility.DisplayDialog("Delete zip file", "Do you want to delete the zip file?", "Yes", "No"))
            {
                File.Delete(downloadFilePath);
            }
            
            Debug.Log($"Download and extract the Main-net store finished. Extracted at \"{extractPath}\"");
        }
    }
}

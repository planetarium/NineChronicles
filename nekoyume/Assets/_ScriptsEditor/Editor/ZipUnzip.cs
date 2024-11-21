using System.IO;
using Ionic.Zip;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class ZipUnzip
    {
        public static void Unzip(string filePath, string unzipPath)
        {
            if (!File.Exists(filePath))
            {
                Debug.Log($"File not exist: {filePath}");
                return;
            }

            EditorUtility.DisplayProgressBar("Unzip", $"path: {unzipPath}", 0f);

            using var zipFile = new ZipFile(filePath);
            var progress = 0f;
            zipFile.ExtractProgress += (sender, args) =>
            {
                // NOTE: Sometimes the next equal 0.
                var next = (float)args.EntriesExtracted / args.EntriesTotal;
                if (next > 0f)
                {
                    progress = next;
                }

                EditorUtility.DisplayProgressBar("Unzip", $"path: {unzipPath}", progress);
            };
            zipFile.ExtractAll(unzipPath);
            EditorUtility.ClearProgressBar();
        }
    }
}

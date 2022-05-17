using System.IO;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class CompileHelper
    {
        private const string CleanupUselessObjFilesMenu =
            "Tools/Compile Helper/Cleanup Useless Obj Files";

        public static readonly string[] UselessObjDirectoryPaths = {
            Path.Combine(Application.dataPath, "_Scripts", "Lib9c", "lib9c", "Lib9c.DevExtensions", "obj"),
        };

        [MenuItem(CleanupUselessObjFilesMenu, true)]
        public static bool IsNotPlayMode() => !Application.isPlaying;
        
        [MenuItem(CleanupUselessObjFilesMenu)]
        public static void CleanupUselessObjFiles()
        {
            foreach (var uselessObjDirectoryPath in UselessObjDirectoryPaths)
            {
                if (Directory.Exists(uselessObjDirectoryPath))
                {
                    Directory.Delete(uselessObjDirectoryPath, true);
                }
            }
            
            // AssetDatabase.Refresh();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"\"{CleanupUselessObjFilesMenu}\" Finished.");
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All(Editor)")]
        public static void DeleteAllEditor()
        {
            var path = Path.Combine(Application.persistentDataPath, "../");
            DeleteAll(path);
        }
        
        [MenuItem("Tools/Libplanet/Delete All(Player)")]
        public static void DeleteAllPlayer()
        {
            var path = Path.Combine(Application.persistentDataPath, $"../../{PlayerSettings.applicationIdentifier}");
            DeleteAll(path);
        }

        private static void DeleteAll(string path)
        {
            var info = new DirectoryInfo(path);
            info.Delete(true);
            PlayerPrefs.DeleteAll();
        }
    }   
}

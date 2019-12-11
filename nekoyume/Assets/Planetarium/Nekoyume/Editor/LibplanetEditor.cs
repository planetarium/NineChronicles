using System.IO;
using UnityEditor;
using Nekoyume.BlockChain;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All(Editor)")]
        public static void DeleteAllEditor()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Development));
        }
        
        [MenuItem("Tools/Libplanet/Delete All(Player)")]
        public static void DeleteAllPlayer()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Production));
        }

        private static void DeleteAll(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }   
}

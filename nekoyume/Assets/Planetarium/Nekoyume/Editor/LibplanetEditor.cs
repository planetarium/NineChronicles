using System.IO;
using Nekoyume.BlockChain;
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

        [MenuItem("Tools/Libplanet/Mine Genesis Block")]
        public static void MineGenesisBlock()
        {
            var path = EditorUtility.SaveFilePanel("Choose path to export the new genesis block", Application.streamingAssetsPath,
                BlockHelper.GenesisBlockPathProd, "");

            if (!EditorUtility.DisplayDialog($"Export the new genesis block?",
                    $"Do you want to export the new genesis block to\n{path}?",
                    "yes", "no"))
            {
                return;
            }
            var block = BlockHelper.MineGenesisBlock();
            BlockHelper.ExportBlock(block, path);
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

using System.IO;
using Nekoyume.BlockChain;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All(Editor) - Mine Genesis Block For Dev To StreamingAssets Folder")]
        public static void DeleteAllEditorAndMinGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Development));
            var path = Path.Combine(Application.streamingAssetsPath, BlockHelper.GenesisBlockNameDev);
            MakeGenesisBlock(path);
        }

        [MenuItem("Tools/Libplanet/Delete All(Player) - Mine Genesis Block For Prod To StreamingAssets Folder")]
        public static void DeleteAllPlayer()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Production));
            var path = Path.Combine(Application.streamingAssetsPath, BlockHelper.GenesisBlockNameProd);
            MakeGenesisBlock(path);
        }

        [MenuItem("Tools/Libplanet/Mine Genesis Block")]
        public static void MineGenesisBlock()
        {
            var path = EditorUtility.SaveFilePanel("Choose path to export the new genesis block",
                Application.streamingAssetsPath,
                BlockHelper.GenesisBlockNameProd, "");

            if (path == "")
            {
                return;
            }

            MakeGenesisBlock(path);
        }

        private static void DeleteAll(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static void MakeGenesisBlock(string path)
        {
            var block = BlockHelper.MineGenesisBlock();
            BlockHelper.ExportBlock(block, path);
        }
    }
}

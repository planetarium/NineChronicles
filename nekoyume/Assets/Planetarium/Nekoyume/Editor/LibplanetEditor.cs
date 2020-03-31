using System.IO;
using Nekoyume.BlockChain;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All(Editor) - Make Genesis Block For Dev To StreamingAssets Folder")]
        public static void DeleteAllEditorAndMakeGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Development));
            MakeGenesisBlock(BlockHelper.GenesisBlockPath);
        }

        [MenuItem("Tools/Libplanet/Delete All(Player) - Make Genesis Block For Prod To StreamingAssets Folder")]
        public static void DeleteAllPlayerAndMakeGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Production));
            MakeGenesisBlock(BlockHelper.GenesisBlockPath);
        }

        [MenuItem("Tools/Libplanet/Make Genesis Block")]
        public static void MakeGenesisBlock()
        {
            var path = EditorUtility.SaveFilePanel(
                "Choose path to export the new genesis block",
                Application.streamingAssetsPath,
                BlockHelper.GenesisBlockName,
                ""
            );

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

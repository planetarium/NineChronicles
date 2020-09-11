using System.Collections.Generic;
using System.IO;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.Model.State;
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
            MakeGenesisBlock(BlockManager.GenesisBlockPath);
        }

        [MenuItem("Tools/Libplanet/Delete All(Player) - Make Genesis Block For Prod To StreamingAssets Folder")]
        public static void DeleteAllPlayerAndMakeGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Production));
            MakeGenesisBlock(BlockManager.GenesisBlockPath);
        }

        [MenuItem("Tools/Libplanet/Make Genesis Block")]
        public static void MakeGenesisBlock()
        {
            var path = EditorUtility.SaveFilePanel(
                "Choose path to export the new genesis block",
                Application.streamingAssetsPath,
                BlockManager.GenesisBlockName,
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
            CreateActivationKey(
                out List<PendingActivationState> pendingActivationStates,
                out List<ActivationKey> activationKeys,
                10);
            activationKeys.ForEach(x => Debug.Log(x.Encode()));

            var block = BlockManager.MineGenesisBlock(pendingActivationStates.ToArray());
            BlockManager.ExportBlock(block, path);
        }

        private static void CreateActivationKey(
            out List<PendingActivationState> pendingActivationStates,
            out List<ActivationKey> activationKeys,
            int countOfKeys)
        {
            pendingActivationStates = new List<PendingActivationState>();
            activationKeys = new List<ActivationKey>();

             for (int i = 0; i < countOfKeys; i++)
             {
                 var pendingKey = new PrivateKey();
                 var nonce = pendingKey.PublicKey.ToAddress().ToByteArray();
                 (ActivationKey ak, PendingActivationState s) =
                     ActivationKey.Create(pendingKey, nonce);
                 pendingActivationStates.Add(s);
                 activationKeys.Add(ak);
             }
        }
    }
}

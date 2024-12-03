using System.Collections.Generic;
using System.IO;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Blockchain;
using Nekoyume.Model;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        private static PrivateKey GetOrCreateInitialValidator()
        {
            var pk = Agent.ProposerKey;
            NcDebug.Log($"Private Key of initialValidator: {pk.ToHexWithZeroPaddings()}");
            NcDebug.Log($"Public Key of initialValidator: {pk.PublicKey}");
            return pk;
        }

        [MenuItem("Tools/Libplanet/Delete All(Editor) - Make Genesis Block For Dev To StreamingAssets Folder")]
        public static void DeleteAllEditorAndMakeGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Development));
            MakeGenesisBlock(BlockManager.GenesisBlockPath(), GetOrCreateInitialValidator());
        }

        [MenuItem("Tools/Libplanet/Delete All(Player) - Make Genesis Block For Prod To StreamingAssets Folder")]
        public static void DeleteAllPlayerAndMakeGenesisBlock()
        {
            DeleteAll(StorePath.GetDefaultStoragePath(StorePath.Env.Production));
            MakeGenesisBlock(BlockManager.GenesisBlockPath());
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
                Directory.Delete(path, true);
            }
        }

        private static void MakeGenesisBlock(string path, PrivateKey proposer = null)
        {
            CreateActivationKey(
                out var pendingActivationStates,
                out var activationKeys,
                10);
            activationKeys.ForEach(x => NcDebug.Log(x.Encode()));

            var block = BlockManager.ProposeGenesisBlock(
                pendingActivationStates.ToArray(),
                proposer ?? Agent.ProposerKey);
            BlockManager.ExportBlock(block, path);
        }

        private static void CreateActivationKey(
            out List<PendingActivationState> pendingActivationStates,
            out List<ActivationKey> activationKeys,
            int countOfKeys)
        {
            pendingActivationStates = new List<PendingActivationState>();
            activationKeys = new List<ActivationKey>();

            for (var i = 0; i < countOfKeys; i++)
            {
                var pendingKey = new PrivateKey();
                var nonce = pendingKey.PublicKey.Address.ToByteArray();
                (var ak, var s) =
                    ActivationKey.Create(pendingKey, nonce);
                pendingActivationStates.Add(s);
                activationKeys.Add(ak);
            }
        }
    }
}

using System;
using System.IO;
using System.Net;
using Bencodex;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public static class BlockManager
    {
        // The file name of the Genesis block to be used in an environment other than Editor.
        // If you modify this value, you have to modify it with entrypoint.sh.
        public const string GenesisBlockName = "genesis-block";

        private static readonly Codec _codec = new Codec();

        public static string GenesisBlockPath => BlockPath(GenesisBlockName);

        /// <summary>
        /// Encodes the block and exports it to a file.
        /// </summary>
        /// <param name="path">The file path where the block will be saved.</param>
        public static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            var dict = block.MarshalBlock();
            var encoded = _codec.Encode(dict);
            File.WriteAllBytes(path, encoded);
        }

        /// <summary>
        /// Read a block from a file.
        /// </summary>
        /// <param name="path">The file path where the block is stored</param>
        /// <returns>Read block object</returns>
        public static Block<PolymorphicAction<ActionBase>> ImportBlock(string path)
        {
            var agent = Game.Game.instance.Agent;
            if (File.Exists(path))
            {
                var buffer = File.ReadAllBytes(path);
                var dict = (Bencodex.Types.Dictionary)_codec.Decode(buffer);
                HashAlgorithmGetter hashAlgorithmGetter = agent.BlockPolicySource
                    .GetPolicy(5_000_000,
                        100) // FIXME: e.g., GetPolicy(IAgent.GetMinimumDifficulty(), IAgent.GetMaxTxCount())
                    .GetHashAlgorithm;
                return BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>(
                    hashAlgorithmGetter, dict);
            }

            var uri = new Uri(path);
            using var client = new WebClient();
            {
                var rawGenesisBlock = client.DownloadData(uri);
                var dict = (Bencodex.Types.Dictionary)_codec.Decode(rawGenesisBlock);
                HashAlgorithmGetter hashAlgorithmGetter = agent.BlockPolicySource
                    .GetPolicy(5_000_000,
                        100) // FIXME: e.g., GetPolicy(IAgent.GetMinimumDifficulty(), IAgent.GetMaxTxCount())
                    .GetHashAlgorithm;
                return BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>(
                    hashAlgorithmGetter, dict);
            }
        }

        public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock(
            PendingActivationState[] pendingActivationStates)
        {
            var tableSheets = Game.Game.GetTableCsvAssets();
            var goldDistributionCsvPath =
                Path.Combine(Application.streamingAssetsPath, "GoldDistribution.csv");
            var goldDistributions =
                GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            return BlockHelper.MineGenesisBlock(
                tableSheets,
                goldDistributions,
                pendingActivationStates,
                new AdminState(new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"), 1500000),
                isActivateAdminAddress: false);
        }

        public static string BlockPath(string filename) =>
            Path.Combine(Application.streamingAssetsPath, filename);
    }
}

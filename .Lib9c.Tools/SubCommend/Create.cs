using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cocona;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Lib9c.Tools.SubCommend
{
    public class Create
    {
        public void Genesis(
            [Option('g')] string gameConfigDir,
            [Option('d')] string goldDistributedPath,
            [Option('a')] int activationKeyCount)
        {
            Dictionary<string, string> tableSheets = Utils.ImportSheets(gameConfigDir);
            Utils.CreateActivationKey(
                out List<PendingActivationState> pendingActivationStates,
                out List<ActivationKey> activationKeys,
                activationKeyCount);
            GoldDistribution[] goldDistributions = GoldDistribution
                .LoadInDescendingEndBlockOrder(goldDistributedPath);
            
            Block<PolymorphicAction<ActionBase>> block = BlockHelper.MineGenesisBlock(
                tableSheets,
                goldDistributions,
                pendingActivationStates.ToArray(),
                new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                isActivateAdminAddress: activationKeyCount != 0);
            
            ExportBlock(block, "genesis-block");
            ExportKeys(activationKeys, "keys.txt");
        }

        private static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            byte[] encoded = block.Serialize();
            File.WriteAllBytes(path, encoded);
        }

        private static void ExportKeys(List<ActivationKey> keys, string path)
        {
            File.WriteAllLines(path, keys.Select(v => v.Encode()));
        }
    }
}

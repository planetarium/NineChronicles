using System.Collections.Generic;
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
                pendingActivationStates.ToArray());
            
            Utils.ExportBlock(block, "genesis-block");
            Utils.ExportKeys(activationKeys, "keys.txt");
        }
    }
}

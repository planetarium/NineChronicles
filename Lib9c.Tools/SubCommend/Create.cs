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
            CreateActivationKey(
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

        private void CreateActivationKey(
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

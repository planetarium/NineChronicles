using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Cocona;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Lib9c.Tools.SubCommend
{
    public class Create
    {
        public void Genesis(
            [Option('g', Description = "/path/to/nekoyume-unity/nekoyume/Assets/AddressableAssets/TableCSV")]
            string gameConfigDir,
            [Option('d', Description = "/path/to/nekoyume-unity/nekoyume/Assets/StreamingAssets/GoldDistribution.csv")]
            string goldDistributedPath,
            [Option('a', Description = "Number of activation keys to generate")]
            uint activationKeyCount,
            [Option("adminStateConfig", Description = "Config path to create AdminState")]
            string adminStateConfigPath,
            [Option("activatedAccountsList", Description = "List of accounts to be activated")]
            string activatedAccountsListPath = null,
            [Option('m', Description = "Config path to create AuthorizedMinersState")]
            string authorizedMinerConfigPath = null

        )
        {
            Dictionary<string, string> tableSheets = Utils.ImportSheets(gameConfigDir);
            Utils.CreateActivationKey(
                out List<PendingActivationState> pendingActivationStates,
                out List<ActivationKey> activationKeys,
                activationKeyCount);
            GoldDistribution[] goldDistributions = GoldDistribution
                .LoadInDescendingEndBlockOrder(goldDistributedPath);

            AdminState adminState = Utils.GetAdminState(adminStateConfigPath);

            AuthorizedMinersState authorizedMinersState = null;
            if (!(authorizedMinerConfigPath is null))
            {
                authorizedMinersState = Utils.GetAuthorizedMinersState(authorizedMinerConfigPath);
            }

            var activatedAccounts = activatedAccountsListPath is null
                ? ImmutableHashSet<Address>.Empty
                : Utils.GetActivatedAccounts(activatedAccountsListPath);

            Block<PolymorphicAction<ActionBase>> block = BlockHelper.MineGenesisBlock(
                tableSheets,
                goldDistributions,
                pendingActivationStates.ToArray(),
                adminState,
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAccounts,
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

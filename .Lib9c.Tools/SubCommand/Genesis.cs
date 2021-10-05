using Bencodex;
using Bencodex.Types;
using Cocona;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Lib9c.Tools.SubCommand
{
    public class Genesis
    {
        private static readonly Codec Codec = new Codec();

        [Command(Description = "Create a new genesis block.")]
        public void Create(
            [Option("private-key", new[]{ 'p' }, Description = "Hex encoded private key for gensis block")]
            string privateKeyHex,
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
            string authorizedMinerConfigPath = null,
            [Option('c', Description = "Path of a plain text file containing names for credits.")]
            string creditsPath = null
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
                isActivateAdminAddress: activationKeyCount != 0,
                credits: creditsPath is null ? null : File.ReadLines(creditsPath),
                privateKey: new PrivateKey(ByteUtil.ParseHex(privateKeyHex))
            );

            ExportBlock(block, "genesis-block");
            ExportKeys(activationKeys, "keys.txt");
        }

        private static void ExportBlock(Block<PolymorphicAction<ActionBase>> block, string path)
        {
            Bencodex.Types.Dictionary dict = block.MarshalBlock();
            byte[] encoded = Codec.Encode(dict);
            File.WriteAllBytes(path, encoded);
        }

        private static void ExportKeys(List<ActivationKey> keys, string path)
        {
            File.WriteAllLines(path, keys.Select(v => v.Encode()));
        }
    }
}

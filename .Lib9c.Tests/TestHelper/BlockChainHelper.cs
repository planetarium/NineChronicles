namespace Lib9c.Tests.TestHelper
{
    using System;
    using System.Collections.Immutable;
    using Lib9c.Renderer;
    using Libplanet;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public static class BlockChainHelper
    {
        public static BlockChain<NCAction> MakeBlockChain(
            BlockRenderer[] blockRenderers,
            IBlockPolicy<NCAction> policy = null,
            IStagePolicy<NCAction> stagePolicy = null,
            IStore store = null,
            IStateStore stateStore = null)
        {
            PrivateKey adminPrivateKey = new PrivateKey();

            policy ??= new BlockPolicy<NCAction>();
            stagePolicy ??= new VolatileStagePolicy<NCAction>();
            store ??= new DefaultStore(null);
            stateStore ??= new TrieStateStore(new DefaultKeyValueStore(null));
            Block<NCAction> genesis = MakeGenesisBlock(adminPrivateKey.ToAddress(), ImmutableHashSet<Address>.Empty);
            return new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis, renderers: blockRenderers );
        }

        public static Block<NCAction> MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            DateTimeOffset? timestamp = null,
            PendingActivationState[] pendingActivations = null
        )
        {
            PrivateKey privateKey = new PrivateKey();
            if (pendingActivations is null)
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(privateKey, nonce);
                pendingActivations = new[] { pendingActivation };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.MineGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue);
        }
    }
}

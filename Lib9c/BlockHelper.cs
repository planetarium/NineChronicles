using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume
{
    public static class BlockHelper
    {
        public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock(
            IDictionary<string, string> tableSheets,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            Address adminAddress,
            AuthorizedMinersState authorizedMinersState = null,
            bool isActivateAdminAddress = false
        )
        {
            if (!tableSheets.TryGetValue(nameof(GameConfigSheet), out var csv))
            {
                throw new KeyNotFoundException(nameof(GameConfigSheet));
            }
            var gameConfigState = new GameConfigState(csv);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(tableSheets[nameof(RedeemCodeListSheet)]);

            // FIXME Must use a separate key for the mainnet.
            var minterKey = new PrivateKey();
            var ncg = new Currency("NCG", 2, minterKey.ToAddress());
            var initialStatesAction = new InitializeStates
            {
                RankingState = new RankingState(),
                ShopState = new ShopState(),
                TableSheets = (Dictionary<string, string>) tableSheets,
                GameConfigState = gameConfigState,
                RedeemCodeState = new RedeemCodeState(redeemCodeListSheet),
                AdminAddressState = new AdminState(adminAddress, 1500000),
                ActivatedAccountsState = new ActivatedAccountsState(
                    isActivateAdminAddress
                    ? ImmutableHashSet<Address>.Empty.Add(adminAddress)
                    : ImmutableHashSet<Address>.Empty),
                GoldCurrencyState = new GoldCurrencyState(ncg),
                GoldDistributions = goldDistributions,
                PendingActivationStates = pendingActivationStates,
                AuthorizedMinersState = authorizedMinersState,
            };
            var actions = new PolymorphicAction<ActionBase>[]
            {
                initialStatesAction,
            };
            var blockAction = new BlockPolicySource(Log.Logger).GetPolicy(5000000).BlockAction;
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    actions,
                    privateKey: minterKey,
                    blockAction: blockAction);
        }
    }
}

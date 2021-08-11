using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
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
            AdminState adminState,
            AuthorizedMinersState authorizedMinersState = null,
            IImmutableSet<Address> activatedAccounts = null,
            bool isActivateAdminAddress = false,
            IEnumerable<string> credits = null,
            int maximumTransactions = 100,
            PrivateKey privateKey = null,
            DateTimeOffset? timestamp = null
        )
        {
            if (!tableSheets.TryGetValue(nameof(GameConfigSheet), out var csv))
            {
                throw new KeyNotFoundException(nameof(GameConfigSheet));
            }
            var gameConfigState = new GameConfigState(csv);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(tableSheets[nameof(RedeemCodeListSheet)]);

            if (privateKey is null)
            {
                privateKey = new PrivateKey();
            }

            var ncg = new Currency("NCG", 2, privateKey.ToAddress());
            activatedAccounts = activatedAccounts ?? ImmutableHashSet<Address>.Empty;
            var initialStatesAction = new InitializeStates
            (
                rankingState: new RankingState(),
                shopState: new ShopState(),
                tableSheets: (Dictionary<string, string>) tableSheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: adminState,
                activatedAccountsState: new ActivatedAccountsState(
                    isActivateAdminAddress
                    ? activatedAccounts.Add(adminState.AdminAddress)
                    : activatedAccounts),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: pendingActivationStates,
                authorizedMinersState: authorizedMinersState,
                creditsState: credits is null ? null : new CreditsState(credits) 
            );
            var actions = new PolymorphicAction<ActionBase>[]
            {
                initialStatesAction,
            };
            var blockAction = new BlockPolicySource(Log.Logger).GetPolicy(5000000, maximumTransactions).BlockAction;
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    HashAlgorithmType.Of<SHA256>(),
                    actions,
                    privateKey: privateKey,
                    blockAction: blockAction,
                    timestamp: timestamp);
        }
    }
}

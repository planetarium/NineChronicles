using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.BlockChain.Policy;
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
            AdminState adminState = null,
            AuthorizedMinersState authorizedMinersState = null,
            IImmutableSet<Address> activatedAccounts = null,
            bool isActivateAdminAddress = false,
            IEnumerable<string> credits = null,
            PrivateKey privateKey = null,
            DateTimeOffset? timestamp = null,
            IEnumerable<ActionBase> actionBases = null
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, privateKey.ToAddress());
#pragma warning restore CS0618
            activatedAccounts = activatedAccounts ?? ImmutableHashSet<Address>.Empty;
            var initialStatesAction = new InitializeStates
            (
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: (Dictionary<string, string>) tableSheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: adminState,
                activatedAccountsState: new ActivatedAccountsState(
                    isActivateAdminAddress && !(adminState is null)
                    ? activatedAccounts.Add(adminState.AdminAddress)
                    : activatedAccounts),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: pendingActivationStates,
                authorizedMinersState: authorizedMinersState,
                creditsState: credits is null ? null : new CreditsState(credits)
            );
            List<PolymorphicAction<ActionBase>> actions = new List<PolymorphicAction<ActionBase>>
            {
                initialStatesAction,
            };
            if (!(actionBases is null))
            {
                actions.AddRange(actionBases.Select(actionBase =>
                    new PolymorphicAction<ActionBase>(actionBase)));
            }

            var blockAction = new BlockPolicySource(Log.Logger).GetPolicy().BlockAction;
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    actions,
                    privateKey: privateKey,
                    blockAction: blockAction,
                    timestamp: timestamp);
        }
    }
}

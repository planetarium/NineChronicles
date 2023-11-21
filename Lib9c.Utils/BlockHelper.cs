using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Sys;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class BlockHelper
    {
        public static Block ProposeGenesisBlock(
            IDictionary<string, string> tableSheets,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            AdminState? adminState = null,
            AuthorizedMinersState? authorizedMinersState = null,
            IImmutableSet<Address>? activatedAccounts = null,
            Dictionary<PublicKey, BigInteger>? initialValidators = null,
            bool isActivateAdminAddress = false,
            IEnumerable<string>? credits = null,
            PrivateKey? privateKey = null,
            DateTimeOffset? timestamp = null,
            IEnumerable<ActionBase>? actionBases = null,
            Currency? goldCurrency = null,
            ISet<Address>? assetMinters = null
        )
        {
            if (!tableSheets.TryGetValue(nameof(GameConfigSheet), out var csv))
            {
                throw new KeyNotFoundException(nameof(GameConfigSheet));
            }
            var gameConfigState = new GameConfigState(csv);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(tableSheets[nameof(RedeemCodeListSheet)]);

            privateKey ??= new PrivateKey();
            initialValidators ??= new Dictionary<PublicKey, BigInteger>();
            activatedAccounts ??= ImmutableHashSet<Address>.Empty;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            goldCurrency ??= Currency.Legacy("NCG", 2, privateKey.ToAddress());
#pragma warning restore CS0618

            var initialStatesAction = new InitializeStates
            (
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: (Dictionary<string, string>) tableSheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: adminState,
                activatedAccountsState: new ActivatedAccountsState(
                    isActivateAdminAddress && !(adminState is null)  // Can't use 'not pattern' due to Unity
                    ? activatedAccounts.Add(adminState.AdminAddress)
                    : activatedAccounts),
                goldCurrencyState: new GoldCurrencyState(goldCurrency.Value),
                goldDistributions: goldDistributions,
                pendingActivationStates: pendingActivationStates,
                authorizedMinersState: authorizedMinersState,
                creditsState: credits is null ? null : new CreditsState(credits),
                assetMinters: assetMinters
            );
            List<ActionBase> actions = new List<ActionBase>
            {
                initialStatesAction,
            };
            IEnumerable<IAction> systemActions = new IAction[]
            {
                new Initialize(
                    states: ImmutableDictionary.Create<Address, IValue>(),
                    validatorSet: new ValidatorSet(
                        initialValidators.Select(validator =>
                            new Validator(validator.Key, validator.Value)).ToList()
                    )
                ),
            };
            if (!(actionBases is null))
            {
                actions.AddRange(actionBases);
            }
            var blockAction = new BlockPolicySource().GetPolicy().BlockAction;
            var actionLoader = new NCActionLoader();
            var actionEvaluator = new ActionEvaluator(
                _ => blockAction,
                new TrieStateStore(new MemoryKeyValueStore()),
                actionLoader);
            return
                BlockChain.ProposeGenesisBlock(
                    actionEvaluator,
                    transactions: ImmutableList<Transaction>.Empty
                        .Add(Transaction.Create(
                            0, privateKey, null, actions.ToPlainValues()))
                        .AddRange(systemActions.Select((sa, index) =>
                            Transaction.Create(
                                index + 1, privateKey, null, new [] { sa.PlainValue }))),
                    privateKey: privateKey,
                    timestamp: timestamp);
        }
    }
}

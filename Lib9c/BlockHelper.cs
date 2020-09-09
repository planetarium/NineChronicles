using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class BlockHelper
    {
        public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock(
            IDictionary<string, string> tableSheets,
            GoldDistribution[] goldDistributions)
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
                AdminAddressState = new AdminState(
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    1500000
                ),
                ActivatedAccountsState = new ActivatedAccountsState(),
                GoldCurrencyState = new GoldCurrencyState(ncg),
                GoldDistributions = goldDistributions
            };
            var actions = new PolymorphicAction<ActionBase>[]
            {
                initialStatesAction,
            };
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(actions, privateKey: minterKey);
        }
    }
}

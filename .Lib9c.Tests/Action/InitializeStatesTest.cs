namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class InitializeStatesTest
    {
        private readonly Dictionary<string, string> _sheets;

        public InitializeStatesTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
        }

        [Fact]
        public void Execute()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
            var ncg = new Currency("NCG", 2, minterKey.ToAddress());

            var action = new InitializeStates
            {
                RankingState = new RankingState(),
                ShopState = new ShopState(),
                TableSheets = _sheets,
                GameConfigState = gameConfigState,
                RedeemCodeState = new RedeemCodeState(redeemCodeListSheet),
                AdminAddressState = new AdminState(
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    1500000
                ),
                ActivatedAccountsState = new ActivatedAccountsState(),
                GoldCurrencyState = new GoldCurrencyState(ncg),
                GoldDistributions = goldDistributions,
            };

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                PreviousStates = new State(),
            });

            var addresses = new List<Address>()
            {
                Addresses.Ranking,
                Addresses.Shop,
                Addresses.GameConfig,
                Addresses.RedeemCode,
                Addresses.Admin,
                Addresses.ActivatedAccount,
                Addresses.GoldCurrency,
                Addresses.GoldDistribution,
            };
            addresses.AddRange(_sheets.Select(kv => Addresses.TableSheet.Derive(kv.Key)));

            foreach (var address in addresses)
            {
                Assert.NotNull(genesisState.GetState(address));
            }
        }
    }
}

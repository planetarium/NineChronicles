namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class ClaimRaidRewardTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _state;

        public ClaimRaidRewardTest()
        {
            var tableCsv = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(tableCsv);
            _state = new State();
            foreach (var kv in tableCsv)
            {
                _state = _state.SetState(Addresses.GetSheetAddress(kv.Key), kv.Value.Serialize());
            }
        }

        [Theory]
        // rank 0
        [InlineData(typeof(NotEnoughRankException), 0, 0, 0, 0)]
        [InlineData(typeof(NotEnoughRankException), 9_000, 0, 0, 0)]
        // Already Claim.
        [InlineData(typeof(NotEnoughRankException), 10_000, 1, 0, 0)]
        // Skip previous reward.
        [InlineData(null, 2_055_000, 1, 645_000, 1_100)]
        // Claim all reward.
        [InlineData(null, 30_000, 0, 30_000, 50)]
        [InlineData(null, 105_000, 0, 97_500, 150)]
        [InlineData(null, 305_000, 0, 217_500, 350)]
        [InlineData(null, 805_000, 0, 405_000, 650)]
        [InlineData(null, 2_055_000, 0, 675_000, 1_150)]
        public void Execute(Type exc, int highScore, int latestRank, int expectedCrystal, int expectedRune)
        {
            Address avatarAddress = new PrivateKey().ToAddress();
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var raiderState = new RaiderState
            {
                HighScore = highScore,
                LatestRewardRank = latestRank,
            };
            IAccountStateDelta state = _state.SetState(raiderAddress, raiderState.Serialize());
            var randomSeed = 0;

            var action = new ClaimRaidReward(avatarAddress);
            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    Signer = default,
                    BlockIndex = 1,
                    Random = new TestRandom(randomSeed),
                    PreviousStates = state,
                });

                var crystalCurrency = CrystalCalculator.CRYSTAL;
                Assert.Equal(expectedCrystal * crystalCurrency, nextState.GetBalance(default, crystalCurrency));

                var rune = 0;
                var runeIds = _tableSheets.RuneWeightSheet.Values
                        .Where(r => r.BossId == bossRow.BossId)
                        .SelectMany(r => r.RuneInfos.Select(i => i.RuneId)).ToHashSet();
                foreach (var runeId in runeIds)
                {
                    var runeCurrency = RuneHelper.ToCurrency(_tableSheets.RuneSheet[runeId], 0, null);
                    rune += (int)nextState.GetBalance(avatarAddress, runeCurrency).MajorUnit;
                }

                Assert.Equal(expectedRune, rune);
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    Signer = default,
                    BlockIndex = 1,
                    Random = new TestRandom(randomSeed),
                    PreviousStates = state,
                }));
            }
        }
    }
}

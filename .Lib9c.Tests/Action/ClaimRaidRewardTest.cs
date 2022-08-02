namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
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
        [InlineData(null, 50_000, 1, 537_500, 545)]
        // Claim all reward.
        [InlineData(null, 10_000, 0, 25_000, 50)]
        [InlineData(null, 20_000, 0, 81_250, 130)]
        [InlineData(null, 30_000, 0, 181_250, 250)]
        [InlineData(null, 40_000, 0, 337_500, 405)]
        [InlineData(null, 50_000, 0, 562_500, 595)]
        [InlineData(null, 90_000, 0, 562_500, 595)]
        public void Execute(Type exc, int highScore, int latestRank, int expectedCrystal, int expectedRune)
        {
            Address avatarAddress = default;
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var raiderState = new RaiderState
            {
                HighScore = highScore,
                LatestRewardRank = latestRank,
            };
            IAccountStateDelta state = _state.SetState(raiderAddress, raiderState.Serialize());

            var action = new ClaimRaidReward(avatarAddress);
            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    Signer = default,
                    BlockIndex = 1,
                    Random = new TestRandom(),
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
                    Random = new TestRandom(),
                    PreviousStates = state,
                }));
            }
        }
    }
}

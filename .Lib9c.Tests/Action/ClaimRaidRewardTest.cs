namespace Lib9c.Tests.Action
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class ClaimRaidRewardTest
    {
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
            var tableCsv = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(tableCsv);
            IAccountStateDelta state = new State();
            foreach (var kv in tableCsv)
            {
                state = state.SetState(Addresses.GetSheetAddress(kv.Key), kv.Value.Serialize());
            }

            Address avatarAddress = default;
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, 1);
            var raiderState = new RaiderState
            {
                HighScore = highScore,
                LatestRewardRank = latestRank,
            };
            state = state.SetState(raiderAddress, raiderState.Serialize());

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
                for (int i = 800_000; i < 800_003; i++)
                {
                    var runeCurrency = RuneHelper.ToCurrency(i);
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

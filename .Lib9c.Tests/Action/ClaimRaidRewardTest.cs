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
        [InlineData(typeof(NotEnoughRankException), 0, 0)]
        // Already Claim.
        [InlineData(typeof(NotEnoughRankException), 1, 1)]
        // Skip previous reward.
        [InlineData(null, 5, 1)]
        // Claim all reward.
        [InlineData(null, 1, 0)]
        [InlineData(null, 2, 0)]
        [InlineData(null, 3, 0)]
        [InlineData(null, 4, 0)]
        [InlineData(null, 5, 0)]
        public void Execute(Type exc, int rank, int latestRank)
        {
            Address agentAddress = default;
            Address avatarAddress = new PrivateKey().ToAddress();
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var highScore = 0;
            var characterRow = _tableSheets.WorldBossCharacterSheet[bossRow.BossId];
            foreach (var waveInfo in characterRow.WaveStats)
            {
                if (waveInfo.Wave > rank)
                {
                    continue;
                }

                highScore += (int)waveInfo.HP;
            }

            var raiderState = new RaiderState
            {
                HighScore = highScore,
                LatestRewardRank = latestRank,
            };
            IAccountStateDelta state = _state.SetState(raiderAddress, raiderState.Serialize());
            var randomSeed = 0;

            var rows = _tableSheets.WorldBossRankRewardSheet.Values
                .Where(x => x.BossId == bossRow.BossId);
            var expectedCrystal = 0;
            var expectedRune = 0;
            foreach (var row in rows)
            {
                if (row.Rank <= latestRank ||
                    row.Rank > rank)
                {
                    continue;
                }

                expectedCrystal += row.Crystal;
                expectedRune += row.Rune;
            }

            var action = new ClaimRaidReward(avatarAddress);
            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    Signer = agentAddress,
                    BlockIndex = 5055201L,
                    Random = new TestRandom(randomSeed),
                    PreviousStates = state,
                });

                var crystalCurrency = CrystalCalculator.CRYSTAL;
                Assert.Equal(
                    expectedCrystal * crystalCurrency,
                    nextState.GetBalance(agentAddress, crystalCurrency));

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
                    BlockIndex = 5055201L,
                    Random = new TestRandom(randomSeed),
                    PreviousStates = state,
                }));
            }
        }
    }
}

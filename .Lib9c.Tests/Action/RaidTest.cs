namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class RaidTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public RaidTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _goldCurrency = new Currency("NCG", decimalPlaces: 2, minters: null);
        }

        [Theory]
        // Join new raid.
        [InlineData(null, true, true, 0L, true, false, 0, 0L, false, false, 0, false, false)]
        // Refill by interval.
        [InlineData(null, true, true, 300L, false, true, 0, 0L, false, false, 0, false, false)]
        // Refill by NCG.
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 0, false, false)]
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 1, false, false)]
        // Boss level up.
        [InlineData(null, true, true, 200L, false, true, 3, 100L, false, false, 0, true, true)]
        // Boss skip level up.
        [InlineData(null, true, true, 200L, false, true, 3, 100L, false, false, 0, true, false)]
        // AvatarState null.
        [InlineData(typeof(FailedLoadStateException), false, false, 0L, false, false, 0, 0L, false, false, 0, false, false)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, false, 0L, false, false, 0, 0L, false, false, 0, false, false)]
        // Insufficient CRYSTAL.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, false, 0, 0L, false, false, 0, false, false)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, true, 0, 10L, true, false, 0, false, false)]
        // Exceed purchase limit.
        [InlineData(typeof(ExceedTicketPurchaseLimitException), true, true, 0L, false, true, 0, 10L, true, false, 1_000, false, false)]
        // Exceed challenge count.
        [InlineData(typeof(ExceedPlayCountException), true, true, 0L, false, true, 0, 0L, false, false, 0, false, false)]
        public void Execute(
            Type exc,
            bool avatarExist,
            bool stageCleared,
            long blockIndex,
            bool crystalExist,
            bool raiderStateExist,
            int remainChallengeCount,
            long refillBlockIndex,
            bool payNcg,
            bool ncgExist,
            int purchaseCount,
            bool kill,
            bool levelUp
        )
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                PayNcg = payNcg,
            };
            Currency crystal = CrystalCalculator.CRYSTAL;
            int raidId = _tableSheets.WorldBossListSheet.FindRaidIdByBlockIndex(blockIndex);
            Address raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            WorldBossListSheet.Row worldBossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
            var hpSheet = _tableSheets.WorldBossGlobalHpSheet;
            Address bossAddress = Addresses.GetWorldBossAddress(raidId);
            int level = 1;
            if (kill & !levelUp)
            {
                level = hpSheet.OrderedList.Last().Level;
            }

            IAccountStateDelta state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            if (avatarExist)
            {
                var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
                foreach (var equipment in equipments)
                {
                    avatarState.inventory.AddItem(equipment);
                }

                if (stageCleared)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                    }
                }

                if (crystalExist)
                {
                    state = state.MintAsset(_agentAddress, 300 * crystal);
                }

                if (raiderStateExist)
                {
                    var raiderState = new RaiderState();
                    raiderState.RefillBlockIndex = refillBlockIndex;
                    raiderState.RemainChallengeCount = remainChallengeCount;
                    raiderState.TotalScore = 1_000;
                    raiderState.TotalChallengeCount = 1;
                    raiderState.PurchaseCount = purchaseCount;
                    raiderState.Cp = 0;
                    raiderState.Level = 0;
                    raiderState.IconId = 0;
                    raiderState.AvatarNameWithHash = "hash";
                    raiderState.AvatarAddress = _avatarAddress;

                    state = state.SetState(raiderAddress, raiderState.Serialize());
                }

                if (ncgExist)
                {
                    var row = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
                    state = state.MintAsset(_agentAddress, (row.TicketPrice + row.AdditionalTicketPrice * purchaseCount) * _goldCurrency);
                }

                state = state
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());
            }

            if (kill)
            {
                var bossState =
                    new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[level])
                        {
                            CurrentHp = 1,
                            Level = level,
                        };
                state = state.SetState(bossAddress, bossState.Serialize());
            }

            if (exc is null)
            {
                var seed = 0;
                var ctx = new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(seed),
                    Rehearsal = false,
                    Signer = _agentAddress,
                };

                var bossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(ctx.BlockIndex);
                var simulator = new RaidSimulator(
                    bossRow.BossId,
                    new TestRandom(seed),
                    avatarState,
                    action.FoodIds,
                    _tableSheets.GetRaidSimulatorSheets());
                simulator.Simulate();
                var score = simulator.DamageDealt;

                var nextState = action.Execute(ctx);

                Assert.Equal(0 * crystal, nextState.GetBalance(_agentAddress, crystal));
                if (crystalExist)
                {
                    Assert.Equal(300 * crystal, nextState.GetBalance(bossAddress, crystal));
                }

                Assert.True(nextState.TryGetState(raiderAddress, out List rawRaider));
                var raiderState = new RaiderState(rawRaider);
                int expectedTotalScore = raiderStateExist ? 1_000 + score : score;
                int expectedRemainChallenge = payNcg ? 0 : 2;
                int expectedTotalChallenge = raiderStateExist ? 2 : 1;

                Assert.Equal(score, raiderState.HighScore);
                Assert.Equal(expectedTotalScore, raiderState.TotalScore);
                Assert.Equal(expectedRemainChallenge, raiderState.RemainChallengeCount);
                Assert.Equal(expectedTotalChallenge, raiderState.TotalChallengeCount);
                Assert.Equal(1, raiderState.Level);
                Assert.Equal(GameConfig.DefaultAvatarArmorId, raiderState.IconId);
                Assert.True(raiderState.Cp > 0);

                Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
                var bossState = new WorldBossState(rawBoss);
                int expectedLevel = level;
                if (kill & levelUp)
                {
                    expectedLevel++;
                }

                Assert.Equal(expectedLevel, bossState.Level);
                if (kill)
                {
                    Assert.Equal(hpSheet[expectedLevel].Hp, bossState.CurrentHp);
                }
                else
                {
                    Assert.True(bossState.CurrentHp < hpSheet[level].Hp);
                }

                if (payNcg)
                {
                    Assert.Equal(0 * _goldCurrency, nextState.GetBalance(_agentAddress, _goldCurrency));
                    Assert.Equal(purchaseCount + 1, nextState.GetRaiderState(raiderAddress).PurchaseCount);
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _agentAddress,
                }));
            }
        }
    }
}

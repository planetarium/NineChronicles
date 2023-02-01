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

    public class Raid1Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public Raid1Test()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _goldCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Theory]
        // Join new raid.
        [InlineData(null, true, true, true, false, 0, 0L, false, false, 0, false, false, false, Raid.RequiredInterval)]
        // Refill by interval.
        [InlineData(null, true, true, false, true, 0, -WorldBossHelper.RefillInterval, false, false, 0, false, false, false, Raid.RequiredInterval)]
        // Refill by NCG.
        [InlineData(null, true, true, false, true, 0, 200L, true, true, 0, false, false, false, Raid.RequiredInterval)]
        [InlineData(null, true, true, false, true, 0, 200L, true, true, 1, false, false, false, Raid.RequiredInterval)]
        // Boss level up.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, true, false, Raid.RequiredInterval)]
        // Update RaidRewardInfo.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, true, true, Raid.RequiredInterval)]
        // Boss skip level up.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, false, false, Raid.RequiredInterval)]
        // AvatarState null.
        [InlineData(typeof(FailedLoadStateException), false, false, false, false, 0, 0L, false, false, 0, false, false, false, Raid.RequiredInterval)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, false, false, false, 0, 0L, false, false, 0, false, false, false, Raid.RequiredInterval)]
        // Insufficient CRYSTAL.
        [InlineData(typeof(InsufficientBalanceException), true, true, false, false, 0, 0L, false, false, 0, false, false, false, Raid.RequiredInterval)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, false, true, 0, 0L, true, false, 0, false, false, false, Raid.RequiredInterval)]
        // Wait interval.
        [InlineData(typeof(RequiredBlockIntervalException), true, true, false, true, 3, 10L, false, false, 0, false, false, false, Raid.RequiredInterval - 4L)]
        // Exceed purchase limit.
        [InlineData(typeof(ExceedTicketPurchaseLimitException), true, true, false, true, 0, 100L, true, false, 1_000, false, false, false, Raid.RequiredInterval)]
        // Exceed challenge count.
        [InlineData(typeof(ExceedPlayCountException), true, true, false, true, 0, 100L, false, false, 0, false, false, false, Raid.RequiredInterval)]
        public void Execute(
            Type exc,
            bool avatarExist,
            bool stageCleared,
            bool crystalExist,
            bool raiderStateExist,
            int remainChallengeCount,
            long refillBlockIndexOffset,
            bool payNcg,
            bool ncgExist,
            int purchaseCount,
            bool kill,
            bool levelUp,
            bool rewardRecordExist,
            long executeOffset
        )
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var action = new Raid1
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
            Address worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);
            int level = 1;
            if (kill & !levelUp)
            {
                level = hpSheet.OrderedList.Last().Level;
            }

            var fee = _tableSheets.WorldBossListSheet[raidId].EntranceFee;

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
                    var price = _tableSheets.WorldBossListSheet[raidId].EntranceFee;
                    state = state.MintAsset(_agentAddress, price * crystal);
                }

                if (raiderStateExist)
                {
                    var raiderState = new RaiderState();
                    raiderState.RefillBlockIndex = blockIndex + refillBlockIndexOffset;
                    raiderState.RemainChallengeCount = remainChallengeCount;
                    raiderState.TotalScore = 1_000;
                    raiderState.HighScore = 0;
                    raiderState.TotalChallengeCount = 1;
                    raiderState.PurchaseCount = purchaseCount;
                    raiderState.Cp = 0;
                    raiderState.Level = 0;
                    raiderState.IconId = 0;
                    raiderState.AvatarName = "hash";
                    raiderState.AvatarAddress = _avatarAddress;
                    raiderState.UpdatedBlockIndex = blockIndex;

                    state = state.SetState(raiderAddress, raiderState.Serialize());
                }

                if (rewardRecordExist)
                {
                    var rewardRecord = new WorldBossKillRewardRecord
                    {
                        [0] = false,
                    };
                    state = state.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
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
                            CurrentHp = 0,
                            Level = level,
                        };
                state = state.SetState(bossAddress, bossState.Serialize());
            }

            if (exc is null)
            {
                var randomSeed = 0;
                var ctx = new ActionContext
                {
                    BlockIndex = blockIndex + executeOffset,
                    PreviousStates = state,
                    Random = new TestRandom(randomSeed),
                    Rehearsal = false,
                    Signer = _agentAddress,
                };

                var nextState = action.Execute(ctx);

                var random = new TestRandom(randomSeed);
                var bossListRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(ctx.BlockIndex);
                var raidSimulatorSheets = _tableSheets.GetRaidSimulatorSheetsV1();
                var simulator = new RaidSimulatorV1(
                    bossListRow.BossId,
                    random,
                    avatarState,
                    action.FoodIds,
                    raidSimulatorSheets,
                    _tableSheets.CostumeStatSheet);
                simulator.Simulate();
                var score = simulator.DamageDealt;

                Dictionary<Currency, FungibleAssetValue> rewardMap
                    = new Dictionary<Currency, FungibleAssetValue>();
                foreach (var reward in simulator.AssetReward)
                {
                    rewardMap[reward.Currency] = reward;
                }

                if (rewardRecordExist)
                {
                    var bossRow = raidSimulatorSheets.WorldBossCharacterSheet[bossListRow.BossId];
                    Assert.True(state.TryGetState(bossAddress, out List prevRawBoss));
                    var prevBossState = new WorldBossState(prevRawBoss);
                    int rank = WorldBossHelper.CalculateRank(bossRow, raiderStateExist ? 1_000 : 0);
                    var rewards = RuneHelper.CalculateReward(
                        rank,
                        prevBossState.Id,
                        _tableSheets.RuneWeightSheet,
                        _tableSheets.WorldBossKillRewardSheet,
                        _tableSheets.RuneSheet,
                        random
                    );

                    foreach (var reward in rewards)
                    {
                        if (!rewardMap.ContainsKey(reward.Currency))
                        {
                            rewardMap[reward.Currency] = reward;
                        }
                        else
                        {
                            rewardMap[reward.Currency] += reward;
                        }
                    }

                    foreach (var reward in rewardMap)
                    {
                        if (reward.Key.Equals(CrystalCalculator.CRYSTAL))
                        {
                            Assert.Equal(reward.Value, nextState.GetBalance(_agentAddress, reward.Key));
                        }
                        else
                        {
                            Assert.Equal(reward.Value, nextState.GetBalance(_avatarAddress, reward.Key));
                        }
                    }
                }

                if (rewardMap.ContainsKey(crystal))
                {
                    Assert.Equal(rewardMap[crystal], nextState.GetBalance(_agentAddress, crystal));
                }

                if (crystalExist)
                {
                    Assert.Equal(fee * crystal, nextState.GetBalance(bossAddress, crystal));
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
                Assert.Equal(expectedLevel, raiderState.LatestBossLevel);
                if (kill)
                {
                    Assert.Equal(hpSheet[expectedLevel].Hp, bossState.CurrentHp);
                }

                if (payNcg)
                {
                    Assert.Equal(0 * _goldCurrency, nextState.GetBalance(_agentAddress, _goldCurrency));
                    Assert.Equal(purchaseCount + 1, nextState.GetRaiderState(raiderAddress).PurchaseCount);
                }

                Assert.True(nextState.TryGetState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
                var rewardRecord = new WorldBossKillRewardRecord(rawRewardInfo);
                Assert.Contains(expectedLevel, rewardRecord.Keys);
                if (rewardRecordExist)
                {
                    Assert.True(rewardRecord[0]);
                }
                else
                {
                    if (expectedLevel == 1)
                    {
                        Assert.False(rewardRecord[1]);
                    }
                    else
                    {
                        Assert.DoesNotContain(1, rewardRecord.Keys);
                    }
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex + executeOffset,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _agentAddress,
                }));
            }
        }

        [Fact]
        public void Execute_With_Reward()
        {
            var action = new Raid1
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                PayNcg = false,
            };

            var worldBossRow = _tableSheets.WorldBossListSheet.First().Value;
            int raidId = worldBossRow.Id;
            Address raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            Address bossAddress = Addresses.GetWorldBossAddress(raidId);
            Address worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);

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

            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var raiderState = new RaiderState();
            raiderState.RefillBlockIndex = 0;
            raiderState.RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
            raiderState.TotalScore = 1_000;
            raiderState.TotalChallengeCount = 1;
            raiderState.PurchaseCount = 0;
            raiderState.Cp = 0;
            raiderState.Level = 0;
            raiderState.IconId = 0;
            raiderState.AvatarName = "hash";
            raiderState.AvatarAddress = _avatarAddress;
            state = state.SetState(raiderAddress, raiderState.Serialize());

            var rewardRecord = new WorldBossKillRewardRecord
            {
                [1] = false,
            };
            state = state.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());

            state = state
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            var bossState =
                new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[2])
                    {
                        CurrentHp = 0,
                        Level = 2,
                    };
            state = state.SetState(bossAddress, bossState.Serialize());
            var randomSeed = 0;
            var random = new TestRandom(randomSeed);

            var simulator = new RaidSimulatorV1(
                worldBossRow.BossId,
                random,
                avatarState,
                action.FoodIds,
                _tableSheets.GetRaidSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet);
            simulator.Simulate();

            Dictionary<Currency, FungibleAssetValue> rewardMap
                    = new Dictionary<Currency, FungibleAssetValue>();
            foreach (var reward in simulator.AssetReward)
            {
                rewardMap[reward.Currency] = reward;
            }

            List<FungibleAssetValue> killRewards = RuneHelper.CalculateReward(
                0,
                bossState.Id,
                _tableSheets.RuneWeightSheet,
                _tableSheets.WorldBossKillRewardSheet,
                _tableSheets.RuneSheet,
                random
            );

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = worldBossRow.StartedBlockIndex + Raid.RequiredInterval,
                PreviousStates = state,
                Random = new TestRandom(randomSeed),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            Assert.True(nextState.TryGetState(raiderAddress, out List rawRaider));
            var nextRaiderState = new RaiderState(rawRaider);
            Assert.Equal(simulator.DamageDealt, nextRaiderState.HighScore);

            foreach (var reward in killRewards)
            {
                if (!rewardMap.ContainsKey(reward.Currency))
                {
                    rewardMap[reward.Currency] = reward;
                }
                else
                {
                    rewardMap[reward.Currency] += reward;
                }
            }

            foreach (var reward in rewardMap)
            {
                if (reward.Key.Equals(CrystalCalculator.CRYSTAL))
                {
                    Assert.Equal(reward.Value, nextState.GetBalance(_agentAddress, reward.Key));
                }
                else
                {
                    Assert.Equal(reward.Value, nextState.GetBalance(_avatarAddress, reward.Key));
                }
            }

            Assert.Equal(1, nextRaiderState.Level);
            Assert.Equal(GameConfig.DefaultAvatarArmorId, nextRaiderState.IconId);
            Assert.True(nextRaiderState.Cp > 0);
            Assert.Equal(3, nextRaiderState.LatestBossLevel);
            Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
            var nextBossState = new WorldBossState(rawBoss);
            Assert.Equal(3, nextBossState.Level);
            Assert.True(nextState.TryGetState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
            var nextRewardInfo = new WorldBossKillRewardRecord(rawRewardInfo);
            Assert.True(nextRewardInfo[1]);
        }

        [Fact]
        public void Execute_Throw_ActionObsoletedException()
        {
            var action = new Raid1
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                PayNcg = false,
            };
            var row = _tableSheets.WorldBossListSheet.Values.First(r => r.Id > 1);
            long blockIndex = row.StartedBlockIndex;
            int raidId = row.Id;
            Address raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            WorldBossListSheet.Row worldBossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
            Address bossAddress = Addresses.GetWorldBossAddress(raidId);
            Address worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);

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

            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            state = state
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            Assert.Throws<ActionObsoletedException>(() => action.Execute(new ActionContext
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

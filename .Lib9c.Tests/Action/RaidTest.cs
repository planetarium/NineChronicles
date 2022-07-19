namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class RaidTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public RaidTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _goldCurrency = new Currency("NCG", decimalPlaces: 2, minters: null);
        }

        [Theory]
        // Join new raid.
        [InlineData(null, true, true, 0L, true, false, 0, 0L, false, false, 0)]
        // Refill by interval.
        [InlineData(null, true, true, 200L, false, true, 0, 100L, false, false, 0)]
        // Refill by NCG.
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 0)]
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 1)]
        // AvatarState null.
        [InlineData(typeof(FailedLoadStateException), false, false, 0L, false, false, 0, 0L, false, false, 0)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, false, 0L, false, false, 0, 0L, false, false, 0)]
        // Insufficient CRYSTAL.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, false, 0, 0L, false, false, 0)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, true, 0, 10L, true, false, 0)]
        // Exceed purchase limit.
        [InlineData(typeof(ExceedTicketPurchaseLimitException), true, true, 0L, false, true, 0, 10L, true, false, 1_000)]
        // Exceed challenge count.
        [InlineData(typeof(ExceedPlayCountException), true, true, 0L, false, true, 0, 0L, false, false, 0)]
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
            int purchaseCount
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
            IAccountStateDelta state = new State()
                .SetState(Addresses.GetSheetAddress<WorldBossListSheet>(), _tableSheets.WorldBossListSheet.Serialize())
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            if (avatarExist)
            {
                var avatarState = new AvatarState(
                    _avatarAddress,
                    _agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default
                );

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

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _agentAddress,
                });

                Address bossAddress = Addresses.GetWorldBossAddress(raidId);
                Assert.Equal(0 * crystal, nextState.GetBalance(_agentAddress, crystal));
                if (crystalExist)
                {
                    Assert.Equal(300 * crystal, nextState.GetBalance(bossAddress, crystal));
                }

                Assert.True(nextState.TryGetState(raiderAddress, out List rawRaider));
                var raiderState = new RaiderState(rawRaider);
                int expectedTotalScore = raiderStateExist ? 11_000 : 10_000;
                int expectedRemainChallenge = payNcg ? 0 : 2;
                int expectedTotalChallenge = raiderStateExist ? 2 : 1;
                Assert.Equal(10_000, raiderState.HighScore);
                Assert.Equal(expectedTotalScore, raiderState.TotalScore);
                Assert.Equal(expectedRemainChallenge, raiderState.RemainChallengeCount);
                Assert.Equal(expectedTotalChallenge, raiderState.TotalChallengeCount);

                Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
                var bossState = new WorldBossState(rawBoss);
                Assert.Equal(10_000, bossState.CurrentHP);
                Assert.Equal(1, bossState.Level);

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

namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipRuneTest
    {
        private readonly Currency _goldCurrency;

        public EquipRuneTest()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        public IAccountStateDelta Init(out Address agentAddress, out Address avatarAddress, out long blockIndex)
        {
            agentAddress = new PrivateKey().ToAddress();
            avatarAddress = new PrivateKey().ToAddress();
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, new AgentState(agentAddress).Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            return state;
        }

        [Theory]
        [InlineData(BattleType.Adventure, 0)]
        public void Execute(BattleType battleType, int slotIndex)
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(runeId);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(slotIndex, runeId) };
            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = battleType,
                RuneInfos = runeInfos,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = agentAddress,
            };

            state = action.Execute(ctx);
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, battleType);
            if (state.TryGetState(runeSlotStateAddress, out List rawRuneSlotState))
            {
                var runeSlotState = new RuneSlotState(rawRuneSlotState);
                var slot = runeSlotState.GetRuneSlot();
                var equipped = slot[slotIndex].Equipped(out _);
                Assert.True(equipped);
            }

            action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = battleType,
                RuneInfos = new List<RuneSlotInfo>(),
            };

            ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = agentAddress,
            };

            state = action.Execute(ctx);
            if (state.TryGetState(runeSlotStateAddress, out List rawRuneSlotState2))
            {
                var runeSlotState = new RuneSlotState(rawRuneSlotState2);
                var slot = runeSlotState.GetRuneSlot();
                var equipped = slot[slotIndex].Equipped(out _);
                Assert.False(equipped);
            }
        }

        [Fact]
        public void Execute_RuneInfosIsEmptyException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = null,
            };

            Assert.Throws<RuneInfosIsEmptyException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_DuplicatedRuneSlotIndexException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeInfos = new List<RuneSlotInfo>
            {
                new RuneSlotInfo(0, 1),
                new RuneSlotInfo(0, 1),
            };
            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = runeInfos,
            };

            Assert.Throws<DuplicatedRuneSlotIndexException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneListNotFoundException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(1312312);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = new List<RuneSlotInfo> { new RuneSlotInfo(0, 1312312) },
            };

            Assert.Throws<RuneListNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneStateNotFoundException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(0, runeId) };

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = runeInfos,
            };

            Assert.Throws<RuneStateNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotNotFoundException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(runeId);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(99, runeId) };

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = runeInfos,
            };

            Assert.Throws<SlotNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotIsLockedException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(runeId);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(3, runeId) };

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = runeInfos,
            };

            Assert.Throws<SlotIsLockedException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotRuneTypeException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First().Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(runeId);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(1, runeId) };

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Adventure,
                RuneInfos = runeInfos,
            };

            Assert.Throws<SlotRuneTypeException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_IsEquippableRuneException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.Values.First(x => x.UsePlace == 5).Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            if (!state.TryGetState(runeStateAddress, out List rawRuneState))
            {
                var runeState = new RuneState(runeId);
                state = state.SetState(runeStateAddress, runeState.Serialize());
            }

            var runeInfos = new List<RuneSlotInfo> { new RuneSlotInfo(0, runeId) };

            var action = new EquipRune()
            {
                AvatarAddress = avatarAddress,
                BattleType = BattleType.Arena,
                RuneInfos = runeInfos,
            };

            Assert.Throws<IsEquippableRuneException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }
    }
}

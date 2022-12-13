namespace Lib9c.Tests.Action
{
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

    public class UnlockRuneSlotTest
    {
        private readonly Currency _goldCurrency;

        public UnlockRuneSlotTest()
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

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );
            return state.SetState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public void Execute(int slotIndex)
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var gameConfig = state.GetGameConfigState();
            var cost = slotIndex == 1
                ? gameConfig.RuneStatSlotUnlockCost
                : gameConfig.RuneSkillSlotUnlockCost;
            var ncgCurrency = state.GetGoldCurrency();
            state = state.MintAsset(agentAddress, cost * ncgCurrency);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = slotIndex,
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
            var adventureAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            if (state.TryGetState(adventureAddr, out List adventureRaw))
            {
                var s = new RuneSlotState(adventureRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var arenaAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            if (state.TryGetState(arenaAddr, out List arenaRaw))
            {
                var s = new RuneSlotState(arenaRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var raidAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid);
            if (state.TryGetState(raidAddr, out List raidRaw))
            {
                var s = new RuneSlotState(raidRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var balance = state.GetBalance(agentAddress, ncgCurrency);
            Assert.Equal("0", balance.GetQuantityString());
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 1,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<InsufficientBalanceException>(() =>
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
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 99,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = agentAddress,
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
        public void Execute_MismatchRuneSlotTypeException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 0,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<MismatchRuneSlotTypeException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotIsAlreadyUnlockedException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var gameConfig = state.GetGameConfigState();
            var ncgCurrency = state.GetGoldCurrency();
            state = state.MintAsset(agentAddress, gameConfig.RuneStatSlotUnlockCost * ncgCurrency);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 1,
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

            Assert.Throws<SlotIsAlreadyUnlockedException>(() =>
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

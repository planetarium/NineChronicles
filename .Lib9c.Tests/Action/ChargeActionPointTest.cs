namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class ChargeActionPointTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialState;

        public ChargeActionPointTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                actionPoint = 0,
            };
            agent.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new State()
                .SetState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetState(_agentAddress, agent.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void Execute(bool useTradable, bool backward)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (useTradable)
            {
                var apStone = ItemFactory.CreateTradableMaterial(row);
                avatarState.inventory.AddItem(apStone);
            }
            else
            {
                var apStone = ItemFactory.CreateItem(row, new TestRandom());
                avatarState.inventory.AddItem(apStone);
            }

            Assert.Equal(0, avatarState.actionPoint);

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            var gameConfigState = nextState.GetGameConfigState();
            Assert.Equal(gameConfigState.ActionPointMax, nextAvatarState.actionPoint);
        }

        [Theory]
        [InlineData(false, false, false, false,  typeof(FailedLoadStateException))]
        [InlineData(true, false, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, true, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, false, true, true, typeof(ActionPointExceededException))]
        [InlineData(true, true, true, true, typeof(ActionPointExceededException))]
        public void Execute_Throw_Exception(bool useAvatarAddress, bool useTradable, bool enough, bool charge, Type exc)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            Assert.Equal(0, avatarState.actionPoint);

            var avatarAddress = useAvatarAddress ? _avatarAddress : default;
            var state = _initialState;
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = useTradable
                ? ItemFactory.CreateTradableMaterial(row)
                : ItemFactory.CreateMaterial(row);
            if (apStone is TradableMaterial tradableMaterial)
            {
                if (!enough)
                {
                    tradableMaterial.RequiredBlockIndex = 10;
                }
            }

            if (enough)
            {
                avatarState.inventory.AddItem(apStone);
                state = state.SetState(_avatarAddress, avatarState.Serialize());
            }

            if (charge)
            {
                avatarState.actionPoint = state.GetGameConfigState().ActionPointMax;
                state = state.SetState(_avatarAddress, avatarState.Serialize());
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = avatarAddress,
            };

            Assert.Throws(exc, () => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new ChargeActionPoint
            {
                avatarAddress = _avatarAddress,
            };

            var updatedAddresses = new List<Address>()
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}

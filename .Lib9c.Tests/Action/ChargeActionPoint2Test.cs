namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
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

    public class ChargeActionPoint2Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialState;

        public ChargeActionPoint2Test()
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
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool useTradable)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (useTradable)
            {
                var apStone = ItemFactory.CreateTradableMaterial(row);
                avatarState.inventory.AddItem2(apStone);
            }
            else
            {
                var apStone = ItemFactory.CreateItem(row, new TestRandom());
                avatarState.inventory.AddItem2(apStone);
            }

            Assert.Equal(0, avatarState.actionPoint);

            var state = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new ChargeActionPoint2()
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

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var gameConfigState = nextState.GetGameConfigState();
            Assert.Equal(gameConfigState.ActionPointMax, nextAvatarState.actionPoint);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new ChargeActionPoint2
            {
                avatarAddress = default,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = new State(),
                    Random = new TestRandom(),
                    Signer = default,
                })
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_Throw_NotEnoughMaterialException(bool useTradable)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            if (useTradable)
            {
                var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                var apStone = ItemFactory.CreateTradableMaterial(row);
                apStone.RequiredBlockIndex = 10;
                avatarState.inventory.AddItem2(apStone);
            }

            Assert.Equal(0, avatarState.actionPoint);

            var state = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new ChargeActionPoint2()
            {
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                })
            );
        }
    }
}

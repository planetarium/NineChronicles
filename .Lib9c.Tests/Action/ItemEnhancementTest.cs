namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    // FIXME: Should work without .csv files
    public class ItemEnhancementTest : IDisposable
    {
        private readonly IRandom _random;
        private TableSheetsState _tableSheetsState;

        public ItemEnhancementTest()
        {
            _tableSheetsState = TableSheetsImporter.ImportTableSheets();
            _random = new TestRandom();
        }

        public void Dispose()
        {
            _tableSheetsState = null;
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(agentAddress);

            var tableSheets = TableSheets.FromTableSheetsState(_tableSheetsState);
            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets,
                new GameConfigState()
            );

            agentState.avatarAddresses.Add(0, avatarAddress);

            var row = tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, 0);
            var materialId = Guid.NewGuid();
            var material = (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, 0);
            avatarState.inventory.AddItem(equipment, 1);
            avatarState.inventory.AddItem(material, 1);

            avatarState.worldInformation.ClearStage(1, 1, 1, new WorldUnlockSheet());

            var slotAddress =
                avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));

            Assert.Equal(0, equipment.level);

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(agentAddress, agentState.Serialize())
                .Add(avatarAddress, avatarState.Serialize())
                .Add(slotAddress, new CombinationSlotState(slotAddress, 0).Serialize())
                .Add(_tableSheetsState.address, _tableSheetsState.Serialize()));

            var action = new ItemEnhancement()
            {
                itemId = default,
                materialIds = new[] { materialId },
                avatarAddress = avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(avatarAddress, 0);
            var resultEquipment = (Equipment)slotState.Result.itemUsable;
            Assert.Equal(1, resultEquipment.level);
            Assert.Equal(default, resultEquipment.ItemId);
        }

        public class TestRandom : IRandom
        {
            private readonly System.Random _random = new System.Random();

            public int Next()
            {
                return _random.Next();
            }

            public int Next(int maxValue)
            {
                return _random.Next(maxValue);
            }

            public int Next(int minValue, int maxValue)
            {
                return _random.Next(minValue, maxValue);
            }

            public void NextBytes(byte[] buffer)
            {
                _random.NextBytes(buffer);
            }

            public double NextDouble()
            {
                return _random.NextDouble();
            }
        }
    }
}

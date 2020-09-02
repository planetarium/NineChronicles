namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ItemEnhancementTest
    {
        private readonly IRandom _random;
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public ItemEnhancementTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(_sheets);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                new GameConfigState()
            );

            agentState.avatarAddresses.Add(0, avatarAddress);

            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, 0);
            var materialId = Guid.NewGuid();
            var material = (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, 0);
            avatarState.inventory.AddItem(equipment, 1);
            avatarState.inventory.AddItem(material, 1);

            avatarState.worldInformation.ClearStage(1, 1, 1, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);

            var slotAddress =
                avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));

            Assert.Equal(0, equipment.level);

            var state = new State()
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, new CombinationSlotState(slotAddress, 0).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(
                    Addresses.TableSheet.Derive(key),
                    Dictionary.Empty.Add(
                        "csv",
                        value
                    )
                );
            }

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

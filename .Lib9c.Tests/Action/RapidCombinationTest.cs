namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RapidCombinationTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public RapidCombinationTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = default(Address);
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses.Add(0, avatarAddress);

            var material =
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass));
            avatarState.inventory.AddItem(material);

            avatarState.worldInformation.ClearStage(1, 1, 1, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);

            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, gameConfigState.HourglassPerBlock, 0);
            avatarState.inventory.AddItem(equipment);

            var result = new CombinationConsumable.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var requiredBlockIndex = gameConfigState.HourglassPerBlock;
            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);

            var slotAddress =
                avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));
            var slotState = new CombinationSlotState(slotAddress, 1);

            slotState.Update(result, 0, requiredBlockIndex);

            var state = new State()
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }

            var action = new RapidCombination()
            {
                avatarAddress = avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(equipment.ItemId, item.ItemId);
            Assert.Equal(1, item.RequiredBlockIndex);
        }
    }
}

namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ChargeActionPointTest
    {
        private readonly Dictionary<string, string> _sheets;

        public ChargeActionPointTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
        }

        [Fact]
        public void Execute()
        {
            var worldSheet = new WorldSheet();
            worldSheet.Set(_sheets[nameof(WorldSheet)]);
            var questRewardSheet = new QuestRewardSheet();
            questRewardSheet.Set(_sheets[nameof(QuestRewardSheet)]);
            var questItemRewardSheet = new QuestItemRewardSheet();
            questItemRewardSheet.Set(_sheets[nameof(QuestItemRewardSheet)]);
            var equipmentItemRecipeSheet = new EquipmentItemRecipeSheet();
            equipmentItemRecipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var equipmentItemSubRecipeSheet = new EquipmentItemSubRecipeSheet();
            equipmentItemSubRecipeSheet.Set(_sheets[nameof(EquipmentItemSubRecipeSheet)]);
            var questSheet = new QuestSheet();
            questSheet.Set(_sheets[nameof(GeneralQuestSheet)]);
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);
            var materialSheet = new MaterialItemSheet();
            materialSheet.Set(_sheets[nameof(MaterialItemSheet)]);

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                worldSheet,
                questSheet,
                questRewardSheet,
                questItemRewardSheet,
                equipmentItemRecipeSheet,
                equipmentItemSubRecipeSheet,
                gameConfigState
            )
            {
                actionPoint = 0,
            };
            agent.avatarAddresses.Add(0, avatarAddress);

            var apStone = ItemFactory.CreateItem(materialSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone));
            avatarState.inventory.AddItem(apStone);

            Assert.Equal(0, avatarState.actionPoint);

            var state = new State()
                .SetState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetState(agentAddress, agent.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), Dictionary.Empty.Add("csv", value));
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = avatarAddress,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarState(avatarAddress);

            Assert.Equal(gameConfigState.ActionPointMax, nextAvatarState.actionPoint);
        }
    }
}

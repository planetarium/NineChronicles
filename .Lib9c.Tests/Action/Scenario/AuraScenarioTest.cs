namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class AuraScenarioTest
    {
        [Fact]
        public void HackAndSlash()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);
            var avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            agentState.avatarAddresses.Add(0, avatarAddress);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress
            );

            var auraRow =
                tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Aura);
            var aura = ItemFactory.CreateItemUsable(auraRow, Guid.NewGuid(), 0L);
            aura.StatsMap.AddStatAdditionalValue(StatType.CRI, 1);
            var skillRow = tableSheets.SkillSheet[800001];
            var skill = SkillFactory.Get(skillRow, 0, 100, 0, StatType.NONE);
            aura.Skills.Add(skill);
            avatarState.inventory.AddItem(aura);

            IAccountStateDelta initialState = new Tests.Action.MockStateDelta()
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(
                    avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize())
                .SetState(
                    Addresses.GoldCurrency,
                    new GoldCurrencyState(Currency.Legacy("NCG", 2, minters: null)).Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
            foreach (var (key, value) in sheets)
            {
                initialState = initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            Assert.Null(initialState.GetState(itemSlotStateAddress));

            var has = new HackAndSlash
            {
                StageId = 1,
                AvatarAddress = avatarAddress,
                Equipments = new List<Guid>
                {
                    aura.ItemId,
                },
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>(),
            };

            var nextState = has.Execute(new ActionContext
            {
                BlockIndex = 2,
                PreviousState = initialState,
                Random = new TestRandom(),
                Signer = agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
            var equippedItem = Assert.IsType<Aura>(nextAvatarState.inventory.Equipments.First());
            Assert.True(equippedItem.equipped);
            var rawItemSlot = Assert.IsType<List>(nextState.GetState(itemSlotStateAddress));
            var itemSlotState = new ItemSlotState(rawItemSlot);
            var equipmentId = itemSlotState.Equipments.Single();
            Assert.Equal(aura.ItemId, equipmentId);
            var player = new Player(avatarState, tableSheets.GetSimulatorSheets());
            var equippedPlayer = new Player(nextAvatarState, tableSheets.GetSimulatorSheets());
            Assert.Null(player.aura);
            Assert.NotNull(equippedPlayer.aura);
            Assert.Equal(player.ATK + 1, equippedPlayer.ATK);
            Assert.Equal(player.CRI + 1, equippedPlayer.CRI);
        }
    }
}

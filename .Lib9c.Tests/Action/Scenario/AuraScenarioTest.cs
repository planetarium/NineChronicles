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
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialState;
        private readonly Aura _aura;
        private readonly TableSheets _tableSheets;

        public AuraScenarioTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress
            );

            var auraRow =
                _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Aura);
            _aura = (Aura)ItemFactory.CreateItemUsable(auraRow, Guid.NewGuid(), 0L);
            _aura.StatsMap.AddStatAdditionalValue(StatType.CRI, 1);
            var skillRow = _tableSheets.SkillSheet[800001];
            var skill = SkillFactory.Get(skillRow, 0, 100, 0, StatType.NONE);
            _aura.Skills.Add(skill);
            avatarState.inventory.AddItem(_aura);

            _initialState = new Tests.Action.MockStateDelta()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize())
                .SetState(
                    Addresses.GoldCurrency,
                    new GoldCurrencyState(Currency.Legacy("NCG", 2, minters: null)).Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize())
                .MintAsset(new ActionContext(), _agentAddress, Currencies.Crystal * 1);
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void HackAndSlash()
        {
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(_avatarAddress, BattleType.Adventure);
            Assert.Null(_initialState.GetState(itemSlotStateAddress));

            var has = new HackAndSlash
            {
                StageId = 1,
                AvatarAddress = _avatarAddress,
                Equipments = new List<Guid>
                {
                    _aura.ItemId,
                },
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>(),
            };

            var nextState = has.Execute(new ActionContext
            {
                BlockIndex = 2,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });

            var avatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            Assert_Aura(avatarState, nextState, itemSlotStateAddress);
        }

        [Fact]
        public void Raid()
        {
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(_avatarAddress, BattleType.Raid);
            Assert.Null(_initialState.GetState(itemSlotStateAddress));
            var avatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var prevState = _initialState.SetState(
                _avatarAddress.Derive(LegacyWorldInformationKey),
                avatarState.worldInformation.Serialize()
            );

            var raid = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>
                {
                    _aura.ItemId,
                },
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };

            var nextState = raid.Execute(new ActionContext
            {
                BlockIndex = 5045201,
                PreviousState = prevState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });
            Assert_Aura(avatarState, nextState, itemSlotStateAddress);
        }

        private void Assert_Aura(AvatarState avatarState, IAccountStateDelta nextState, Address itemSlotStateAddress)
        {
            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            var equippedItem = Assert.IsType<Aura>(nextAvatarState.inventory.Equipments.First());
            Assert.True(equippedItem.equipped);
            var rawItemSlot = Assert.IsType<List>(nextState.GetState(itemSlotStateAddress));
            var itemSlotState = new ItemSlotState(rawItemSlot);
            var equipmentId = itemSlotState.Equipments.Single();
            Assert.Equal(_aura.ItemId, equipmentId);
            var player = new Player(avatarState, _tableSheets.GetSimulatorSheets());
            var equippedPlayer = new Player(nextAvatarState, _tableSheets.GetSimulatorSheets());
            Assert.Null(player.aura);
            Assert.NotNull(equippedPlayer.aura);
            Assert.Equal(player.ATK + 1, equippedPlayer.ATK);
            Assert.Equal(player.CRI + 1, equippedPlayer.CRI);
        }
    }
}

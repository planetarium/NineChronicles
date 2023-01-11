namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RuneScenarioTest
    {
        [Fact]
        public void Craft_And_Equip()
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

            IAccountStateDelta initialState = new Tests.Action.State()
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

            var runeId = 30001;
            var runeRow = tableSheets.RuneSheet[runeId];
            var rune = RuneHelper.ToCurrency(runeRow, 0, null);
            initialState = initialState.MintAsset(avatarAddress, rune * 1);

            var runeAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            Assert.Null(initialState.GetState(runeAddress));

            var craftAction = new RuneEnhancement
            {
                AvatarAddress = avatarAddress,
                RuneId = runeId,
            };

            var state = craftAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = initialState,
                Random = new TestRandom(),
                Signer = agentAddress,
            });

            var rawRuneState = Assert.IsType<List>(state.GetState(runeAddress));
            var runeState = new RuneState(rawRuneState);
            Assert.Equal(1, runeState.Level);
            Assert.Equal(runeId, runeState.RuneId);

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            Assert.Null(state.GetState(runeSlotStateAddress));

            var has = new HackAndSlash
            {
                StageId = 1,
                AvatarAddress = avatarAddress,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(0, runeId),
                },
            };

            var nextState = has.Execute(new ActionContext
            {
                BlockIndex = 2,
                PreviousStates = state,
                Random = new TestRandom(),
                Signer = agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
            var rawRuneSlot = Assert.IsType<List>(nextState.GetState(runeSlotStateAddress));
            var runeSlot = new RuneSlotState(rawRuneSlot);
            var runeSlotInfo = runeSlot.GetEquippedRuneSlotInfos().Single();

            Assert.Equal(runeId, runeSlotInfo.RuneId);
            Assert.Equal(0, runeSlotInfo.SlotIndex);
        }
    }
}

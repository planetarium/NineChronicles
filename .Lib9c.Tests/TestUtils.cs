namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using static SerializeKeys;
    using State = Lib9c.Tests.Action.State;

    public static class TestUtils
    {
        public static (
            TableSheets tableSheets,
            Address agentAddress,
            Address avatarAddress,
            IAccountStateDelta initialStatesWithAvatarStateV1,
            IAccountStateDelta initialStatesWithAvatarStateV2) InitializeStates()
        {
            IAccountStateDelta states = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                states = states.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var goldCurrency = Currency.Legacy("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states.SetState(
                goldCurrencyState.address,
                goldCurrencyState.Serialize());

            var agentAddr = new PrivateKey().ToAddress();
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, 0);
            var agentState = new AgentState(agentAddr);
            var avatarState = new AvatarState(
                avatarAddr,
                agentAddr,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                avatarAddr.Derive("ranking_map"));
            agentState.avatarAddresses.Add(0, avatarAddr);

            var initialStatesWithAvatarStateV1 = states
                .SetState(agentAddr, agentState.Serialize())
                .SetState(avatarAddr, avatarState.Serialize());
            var initialStatesWithAvatarStateV2 = states
                .SetState(agentAddr, agentState.Serialize())
                .SetState(avatarAddr, avatarState.SerializeV2())
                .SetState(
                    avatarAddr.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddr.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddr.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());

            return (
                tableSheets,
                agentAddr,
                avatarAddr,
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2);
        }

        public static (IAccountStateDelta, IAccountStateDelta) DisableQuestList(
            IAccountStateDelta stateV1,
            IAccountStateDelta stateV2,
            Address avatarAddress
        )
        {
            var emptyQuestList = new QuestList(
                new QuestSheet(),
                new QuestRewardSheet(),
                new QuestItemRewardSheet(),
                new EquipmentItemRecipeSheet(),
                new EquipmentItemSubRecipeSheet()
            );
            var avatarState = stateV1.GetAvatarState(avatarAddress);
            avatarState.questList = emptyQuestList;
            var newStateV1 = stateV1.SetState(avatarAddress, avatarState.Serialize());
            var newStateV2 = stateV2.SetState(
                avatarAddress.Derive(LegacyQuestListKey),
                emptyQuestList.Serialize()
            );
            return (newStateV1, newStateV2);
        }

        public static Dictionary<ItemBase, int> GetMaterialsFromCraftInfo(
            TableSheets tableSheets,
            IEnumerable<(int, int?, int, long)> targetRecipeIdList,
            IRandom random = null
        )
        {
            random ??= new TestRandom();
            var materialDict = new Dictionary<ItemBase, int>();

            foreach (var (recipeId, subRecipeId, _, _) in targetRecipeIdList)
            {
                var itemRecipeRow = tableSheets.EquipmentItemRecipeSheet.OrderedList.First(e =>
                    e.Id == recipeId);
                var materialRow = tableSheets.MaterialItemSheet[itemRecipeRow.MaterialId];
                var material = ItemFactory.CreateItem(materialRow, random);
                if (materialDict.ContainsKey(material))
                {
                    materialDict[material] += itemRecipeRow.MaterialCount;
                }
                else
                {
                    materialDict[material] = itemRecipeRow.MaterialCount;
                }

                if (!(subRecipeId is null))
                {
                    var subRow =
                        tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList.First(e =>
                            e.Id == (int)subRecipeId!);
                    foreach (var materialInfo in subRow.Materials)
                    {
                        var subMaterial = ItemFactory.CreateItem(
                            tableSheets.MaterialItemSheet[materialInfo.Id], random);

                        if (materialDict.ContainsKey(subMaterial))
                        {
                            materialDict[subMaterial] += materialInfo.Count;
                        }
                        else
                        {
                            materialDict[subMaterial] = materialInfo.Count;
                        }
                    }
                }
            }

            return materialDict;
        }

        public static List<(int, int?, int, long)> GetCraftInfoFromItemId(
            TableSheets tableSheets,
            int targetItemId,
            IRandom random = null)
        {
            return GetCraftInfoFromItemId(tableSheets, new[] { targetItemId }, random);
        }

        public static List<(int, int?, int, long)> GetCraftInfoFromItemId(
            TableSheets tableSheets,
            IEnumerable<int> targetItemIdList,
            IRandom random = null
        )
        {
            random ??= new TestRandom();
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var consumableItemSheet = tableSheets.ConsumableItemSheet;

            var recipeIdList = new List<(int, int?, int, long)>();
            foreach (var itemId in targetItemIdList)
            {
                ItemSheet.Row itemRow;
                try
                {
                    itemRow = equipmentItemSheet.First(e => e.Value.Id == itemId).Value;
                }
                catch (InvalidOperationException)
                {
                    itemRow = consumableItemSheet.First(e => e.Value.Id == itemId).Value;
                }

                var itemRecipeRow = tableSheets.EquipmentItemRecipeSheet.First(e =>
                    e.Value.ResultEquipmentId == itemRow.Id).Value;
                recipeIdList.Add((itemRecipeRow.Id, itemRecipeRow.SubRecipeIds?[0],
                    itemRecipeRow.UnlockStage, itemRecipeRow.RequiredBlockIndex));
            }

            return recipeIdList;
        }

        public static string CsvLinqWhere(string csv, Func<string, bool> where)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(where);
            return string.Join('\n', after);
        }

        public static string CsvLinqSelect(string csv, Func<string, string> select)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(select);
            return string.Join('\n', after);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Libplanet;
using Libplanet.Action;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Lib9c.DevExtensions.Model;
using Newtonsoft.Json;

namespace Lib9c.DevExtensions
{
    public static class TestbedHelper
    {
        public readonly struct AddedItemInfo
        {
            public Guid OrderId { get; }
            public Guid TradableId { get; }

            public AddedItemInfo(Guid orderId, Guid tradableId)
            {
                OrderId = orderId;
                TradableId = tradableId;
            }
        }

        public static AvatarState CreateAvatarState(string name,
            Address agentAddress,
            Address avatarAddress,
            long blockIndex,
            AvatarSheets avatarSheets,
            WorldSheet worldSheet,
            GameConfigState gameConfigState,
            Address rankingMapAddress)
        {
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                blockIndex,
                avatarSheets,
                gameConfigState,
                rankingMapAddress,
                name != string.Empty ? name : "testId"
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    worldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };

            return avatarState;
        }

        public static void AddItem(CostumeItemSheet costumeItemSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            MaterialItemSheet materialItemSheet,
            ConsumableItemSheet consumableItemSheet,
            IRandom random,
            Item item,
            AddedItemInfo addedItemInfo,
            AvatarState avatarState)
        {
            switch (item.ItemSubType)
            {
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    if (costumeItemSheet.TryGetValue(item.ID, out var costumeRow))
                    {
                        var costume =
                            ItemFactory.CreateCostume(costumeRow, addedItemInfo.TradableId);
                        avatarState.inventory.AddItem(costume);
                    }

                    break;

                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                    if (equipmentItemSheet.TryGetValue(item.ID, out var equipmentRow))
                    {
                        var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow,
                            addedItemInfo.TradableId,
                            0,
                            item.Level);

                        if (item.OptionIds.Length > 0)
                        {
                            var optionRows = new List<EquipmentItemOptionSheet.Row>();
                            foreach (var optionId in item.OptionIds)
                            {
                                if (!optionSheet.TryGetValue(optionId, out var optionRow))
                                {
                                    continue;
                                }

                                optionRows.Add(optionRow);
                            }

                            AddOption(skillSheet, equipment, optionRows, random);
                        }

                        avatarState.inventory.AddItem(equipment);
                    }

                    break;

                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    if (materialItemSheet.TryGetValue(item.ID, out var materialRow))
                    {
                        var material =
                            ItemFactory.CreateTradableMaterial(materialRow,
                                addedItemInfo.TradableId);
                        avatarState.inventory.AddItem(material, item.Count);
                    }

                    break;

                case ItemSubType.Food:
                    if (consumableItemSheet.TryGetValue(item.ID, out var consumableRow))
                    {
                        var consumable = (Consumable)ItemFactory.CreateItemUsable(consumableRow,
                            addedItemInfo.TradableId,
                            0,
                            item.Level);
                        avatarState.inventory.AddItem(consumable);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void AddOption(
            SkillSheet skillSheet,
            Equipment equipment,
            IEnumerable<EquipmentItemOptionSheet.Row> optionRows,
            IRandom random)
        {
            var optionIds = new HashSet<int>();

            foreach (var optionRow in optionRows.OrderBy(r => r.Id))
            {
                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                    }
                }

                optionIds.Add(optionRow.Id);
            }
        }

        public static T LoadJsonFile<T>(string path)
        {
            var fileStream = new FileStream(path, FileMode.Open);
            var data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();
            var jsonData = Encoding.UTF8.GetString(data);
            var result = JsonConvert.DeserializeObject<T>(jsonData);
            return result;
        }
    }
}

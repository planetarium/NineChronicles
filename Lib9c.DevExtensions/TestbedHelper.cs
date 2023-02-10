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
        public class AddedItemInfo
        {
            public Guid OrderId { get; }
            public Guid TradableId { get; set; }

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
                        var material = ItemFactory.CreateTradableMaterial(materialRow);
                        avatarState.inventory.AddItem(material, item.Count);
                        addedItemInfo.TradableId = material.TradableId;
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

#if UNITY_ANDROID
        private static TestbedCreateAvatar buffer = null;
        public static TestbedCreateAvatar LoadTestbedCreateAvatarForQA()
        {           
            if (buffer is not null)
            {
                return buffer;
            }
            buffer = LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
            return buffer;
        }
#endif

        public static T LoadData<T>(string fileName)
        {
            var path = GetDataPath(fileName);
#if UNITY_ANDROID
            path = Path.Combine(UnityEngine.Application.streamingAssetsPath, fileName);
            path += ".json";
#endif
            var data = LoadJsonFile<T>(path);
            return data;
        }

        public static string GetDataPath(string fileName)
        {
#if UNITY_EDITOR
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets", "_Scripts", "Lib9c", "lib9c", "Lib9c.DevExtensions",
                "Data", $"{fileName}.json");
#elif LIB9C_DEV_EXTENSIONS && UNITY_STANDALONE_WIN
            return Path.Combine(
                $"{Directory.GetCurrentDirectory()}",
                "9c_Data", "StreamingAssets", $"{fileName}.json");
#elif LIB9C_DEV_EXTENSIONS && UNITY_STANDALONE_OSX
            return Path.Combine(
                $"{Directory.GetCurrentDirectory()}", "9c.app", "Contents", "Resources",
                "Data", "StreamingAssets", $"{fileName}.json");
#else
            return Path.Combine("..", "..", "..", "..", "Lib9c.DevExtensions",
                "Data", $"{fileName}.json");
#endif
        }

        private static T LoadJsonFile<T>(string path)
        {
#if UNITY_ANDROID
            UnityEngine.WWW www = new UnityEngine.WWW(path);
            while (!www.isDone)
            {
                // wait for data load
            }
            var output = JsonConvert.DeserializeObject<T>(www.text);
            return output;
#else
            var fileStream = new FileStream(path, FileMode.Open);
            var data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Close();
            var jsonData = Encoding.UTF8.GetString(data);
            var result = JsonConvert.DeserializeObject<T>(jsonData);
            return result;
#endif
        }



    }
}

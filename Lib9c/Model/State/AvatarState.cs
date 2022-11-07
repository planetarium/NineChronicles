using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState : State, ICloneable
    {
        public string name;
        public int characterId;
        public int level;
        public long exp;
        public Inventory inventory;
        public WorldInformation worldInformation;
        // FIXME: it seems duplicated with blockIndex.
        public long updatedAt;
        public Address agentAddress;
        public QuestList questList;
        public MailBox mailBox;
        public long blockIndex;
        public long dailyRewardReceivedIndex;
        public int actionPoint;
        public CollectionMap stageMap;
        public CollectionMap monsterMap;
        public CollectionMap itemMap;
        public CollectionMap eventMap;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public List<Address> combinationSlotAddresses;
        public const int CombinationSlotCapacity = 4;

        public string NameWithHash { get; private set; }
        public int Nonce { get; private set; }

        [Obsolete("don't use this field.")]
        public readonly Address RankingMapAddress;

        public static Address CreateAvatarAddress()
        {
            var key = new PrivateKey();
            return key.PublicKey.ToAddress();
        }

        public AvatarState(Address address,
            Address agentAddress,
            long blockIndex,
            AvatarSheets avatarSheets,
            GameConfigState gameConfigState,
            Address rankingMapAddress,
            string name = null) : base(address)
        {
            this.name = name ?? string.Empty;
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Inventory();
            worldInformation = new WorldInformation(blockIndex, avatarSheets.WorldSheet, GameConfig.IsEditor);
            updatedAt = blockIndex;
            this.agentAddress = agentAddress;
            questList = new QuestList(
                avatarSheets.QuestSheet,
                avatarSheets.QuestRewardSheet,
                avatarSheets.QuestItemRewardSheet,
                avatarSheets.EquipmentItemRecipeSheet,
                avatarSheets.EquipmentItemSubRecipeSheet
            );
            mailBox = new MailBox();
            this.blockIndex = blockIndex;
            actionPoint = gameConfigState.ActionPointMax;
            stageMap = new CollectionMap();
            monsterMap = new CollectionMap();
            itemMap = new CollectionMap();
            const QuestEventType createEvent = QuestEventType.Create;
            const QuestEventType levelEvent = QuestEventType.Level;
            eventMap = new CollectionMap
            {
                new KeyValuePair<int, int>((int) createEvent, 1),
                new KeyValuePair<int, int>((int) levelEvent, level),
            };
            combinationSlotAddresses = new List<Address>(CombinationSlotCapacity);
            for (var i = 0; i < CombinationSlotCapacity; i++)
            {
                var slotAddress = address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                combinationSlotAddresses.Add(slotAddress);
            }

            combinationSlotAddresses = combinationSlotAddresses
                .OrderBy(element => element)
                .ToList();

            RankingMapAddress = rankingMapAddress;
            UpdateGeneralQuest(new[] { createEvent, levelEvent });
            UpdateCompletedQuest();

            PostConstructor();
        }

        public AvatarState(AvatarState avatarState) : base(avatarState.address)
        {
            if (avatarState == null)
                throw new ArgumentNullException(nameof(avatarState));

            name = avatarState.name;
            characterId = avatarState.characterId;
            level = avatarState.level;
            exp = avatarState.exp;
            inventory = avatarState.inventory;
            worldInformation = avatarState.worldInformation;
            updatedAt = avatarState.updatedAt;
            agentAddress = avatarState.agentAddress;
            questList = avatarState.questList;
            mailBox = avatarState.mailBox;
            blockIndex = avatarState.blockIndex;
            dailyRewardReceivedIndex = avatarState.dailyRewardReceivedIndex;
            actionPoint = avatarState.actionPoint;
            stageMap = avatarState.stageMap;
            monsterMap = avatarState.monsterMap;
            itemMap = avatarState.itemMap;
            eventMap = avatarState.eventMap;
            hair = avatarState.hair;
            lens = avatarState.lens;
            ear = avatarState.ear;
            tail = avatarState.tail;
            combinationSlotAddresses = avatarState.combinationSlotAddresses;
            RankingMapAddress = avatarState.RankingMapAddress;

            PostConstructor();
        }

        public AvatarState(Dictionary serialized)
            : base(serialized)
        {
            string nameKey = NameKey;
            string characterIdKey = CharacterIdKey;
            string levelKey = LevelKey;
            string expKey = ExpKey;
            string inventoryKey = LegacyInventoryKey;
            string worldInformationKey = LegacyWorldInformationKey;
            string updatedAtKey = UpdatedAtKey;
            string agentAddressKey = AgentAddressKey;
            string questListKey = LegacyQuestListKey;
            string mailBoxKey = MailBoxKey;
            string blockIndexKey = BlockIndexKey;
            string dailyRewardReceivedIndexKey = DailyRewardReceivedIndexKey;
            string actionPointKey = ActionPointKey;
            string stageMapKey = StageMapKey;
            string monsterMapKey = MonsterMapKey;
            string itemMapKey = ItemMapKey;
            string eventMapKey = EventMapKey;
            string hairKey = HairKey;
            string lensKey = LensKey;
            string earKey = EarKey;
            string tailKey = TailKey;
            string combinationSlotAddressesKey = CombinationSlotAddressesKey;
            string rankingMapAddressKey = RankingMapAddressKey;
            if (serialized.ContainsKey(LegacyNameKey))
            {
                nameKey = LegacyNameKey;
                characterIdKey = LegacyCharacterIdKey;
                levelKey = LegacyLevelKey;
                updatedAtKey = LegacyUpdatedAtKey;
                agentAddressKey = LegacyAgentAddressKey;
                mailBoxKey = LegacyMailBoxKey;
                blockIndexKey = LegacyBlockIndexKey;
                dailyRewardReceivedIndexKey = LegacyDailyRewardReceivedIndexKey;
                actionPointKey = LegacyActionPointKey;
                stageMapKey = LegacyStageMapKey;
                monsterMapKey = LegacyMonsterMapKey;
                itemMapKey = LegacyItemMapKey;
                eventMapKey = LegacyEventMapKey;
                hairKey = LegacyHairKey;
                earKey = LegacyEarKey;
                tailKey = LegacyTailKey;
                combinationSlotAddressesKey = LegacyCombinationSlotAddressesKey;
                rankingMapAddressKey = LegacyRankingMapAddressKey;
            }

            name = serialized[nameKey].ToDotnetString();
            characterId = (int)((Integer)serialized[characterIdKey]).Value;
            level = (int)((Integer)serialized[levelKey]).Value;
            exp = (long)((Integer)serialized[expKey]).Value;
            updatedAt = serialized[updatedAtKey].ToLong();
            agentAddress = serialized[agentAddressKey].ToAddress();
            mailBox = new MailBox((List)serialized[mailBoxKey]);
            blockIndex = (long)((Integer)serialized[blockIndexKey]).Value;
            dailyRewardReceivedIndex = (long)((Integer)serialized[dailyRewardReceivedIndexKey]).Value;
            actionPoint = (int)((Integer)serialized[actionPointKey]).Value;
            stageMap = new CollectionMap((Dictionary)serialized[stageMapKey]);
            serialized.TryGetValue((Text)monsterMapKey, out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Dictionary)value2);
            itemMap = new CollectionMap((Dictionary)serialized[itemMapKey]);
            eventMap = new CollectionMap((Dictionary)serialized[eventMapKey]);
            hair = (int)((Integer)serialized[hairKey]).Value;
            lens = (int)((Integer)serialized[lensKey]).Value;
            ear = (int)((Integer)serialized[earKey]).Value;
            tail = (int)((Integer)serialized[tailKey]).Value;
            combinationSlotAddresses = serialized[combinationSlotAddressesKey].ToList(StateExtensions.ToAddress);
            RankingMapAddress = serialized[rankingMapAddressKey].ToAddress();

            if (serialized.ContainsKey(inventoryKey))
            {
                inventory = new Inventory((List)serialized[inventoryKey]);
            }

            if (serialized.ContainsKey(worldInformationKey))
            {
                worldInformation = new WorldInformation((Dictionary)serialized[worldInformationKey]);
            }

            if (serialized.ContainsKey(questListKey))
            {
                questList = new QuestList((Dictionary)serialized[questListKey]);
            }

            PostConstructor();
        }

        private void PostConstructor()
        {
            NameWithHash = $"{name} <size=80%><color=#A68F7E>#{address.ToHex().Substring(0, 4)}</color></size>";
        }

        public void Update(IStageSimulator stageSimulator)
        {
            var player = stageSimulator.Player;
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldInformation = player.worldInformation;
#pragma warning disable LAA1002
            foreach (var pair in player.monsterMap)
#pragma warning restore LAA1002
            {
                monsterMap.Add(pair);
            }

#pragma warning disable LAA1002
            foreach (var pair in player.eventMap)
#pragma warning restore LAA1002
            {
                eventMap.Add(pair);
            }

            if (stageSimulator.Log.IsClear)
            {
                stageMap.Add(new KeyValuePair<int, int>(stageSimulator.StageId, 1));
            }

#pragma warning disable LAA1002
            foreach (var pair in stageSimulator.ItemMap)
#pragma warning restore LAA1002
            {
                itemMap.Add(pair);
            }

            UpdateStageQuest(stageSimulator.Reward);
        }

        public void Apply(Player player, long blockIndex)
        {
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            updatedAt = blockIndex;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUp();
        }

        [Obsolete("Use Update")]
        public void Update2(Mail.Mail mail)
        {
            mailBox.Add(mail);
        }

        [Obsolete("Use Update")]
        public void Update3(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUp2();
        }

        [Obsolete("No longer in use.")]
        public void UpdateTemp(Mail.Mail mail, long currentBlockIndex)
        {
            mailBox.Add(mail);
            mailBox.CleanUpTemp(currentBlockIndex);
        }

        public void Customize(int hair, int lens, int ear, int tail)
        {
            this.hair = hair;
            this.lens = lens;
            this.ear = ear;
            this.tail = tail;
        }

        public void UpdateGeneralQuest(IEnumerable<QuestEventType> types)
        {
            eventMap = questList.UpdateGeneralQuest(types, eventMap);
        }

        private void UpdateCompletedQuest()
        {
            eventMap = questList.UpdateCompletedQuest(eventMap);
        }

        private void UpdateStageQuest(IEnumerable<ItemBase> items)
        {
            questList.UpdateStageQuest(stageMap);
            questList.UpdateMonsterQuest(monsterMap);
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateGeneralQuest(new[] { QuestEventType.Level, QuestEventType.Die });
            UpdateCompletedQuest();
        }

        public void UpdateFromRapidCombination(CombinationConsumable5.ResultModel result,
            long requiredIndex)
        {
            var mail = mailBox.First(m => m.id == result.id);
            mail.requiredBlockIndex = requiredIndex;
            var item = inventory.Items
                .Select(i => i.item)
                .OfType<ItemUsable>()
                .First(i => i.ItemId == result.itemUsable.ItemId);
            item.Update(requiredIndex);
        }

        public void UpdateFromRapidCombinationV2(RapidCombination5.ResultModel result,
            long requiredIndex)
        {
            var mail = mailBox.First(m => m.id == result.id);
            mail.requiredBlockIndex = requiredIndex;
            var item = inventory.Items
                .Select(i => i.item)
                .OfType<ItemUsable>()
                .First(i => i.ItemId == result.itemUsable.ItemId);
            item.Update(requiredIndex);
        }

        // todo 1: 퀘스트 전용 함수임을 알 수 있는 네이밍이 필요함.
        // todo 2: 혹은 분리된 객체에게 위임하면 좋겠음.
        #region Quest From Action

        public void UpdateFromCombination(ItemUsable itemUsable)
        {
            questList.UpdateCombinationQuest(itemUsable);
            var type = itemUsable.ItemType == ItemType.Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem(itemUsable, false);
        }

        public void UpdateFromCombination2(ItemUsable itemUsable)
        {
            questList.UpdateCombinationQuest(itemUsable);
            var type = itemUsable.ItemType == ItemType.Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem2(itemUsable, false);
        }

        public void UpdateFromItemEnhancement(Equipment equipment)
        {
            questList.UpdateItemEnhancementQuest(equipment);
            var type = QuestEventType.Enhancement;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem(equipment, false);
        }

        public void UpdateFromItemEnhancement2(Equipment equipment)
        {
            questList.UpdateItemEnhancementQuest(equipment);
            var type = QuestEventType.Enhancement;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem2(equipment, false);
        }

        public void UpdateFromAddItem(ItemUsable itemUsable, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemGradeQuest(itemUsable);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }

        [Obsolete("Use UpdateFromAddItem")]
        public void UpdateFromAddItem2(ItemUsable itemUsable, bool canceled)
        {
            var pair = inventory.AddItem2(itemUsable);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemGradeQuest(itemUsable);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }

        public void UpdateFromAddItem(ItemBase itemUsable, int count, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable, count: count);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }

        [Obsolete("Use UpdateFromAddItem")]
        public void UpdateFromAddItem2(ItemBase itemUsable, int count, bool canceled)
        {
            var pair = inventory.AddItem2(itemUsable, count: count);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }

        public void UpdateFromAddCostume(Costume costume, bool canceled)
        {
            var pair = inventory.AddItem2(costume);
            itemMap.Add(pair);
        }

        public void UpdateFromQuestReward(Quest.Quest quest, MaterialItemSheet materialItemSheet)
        {
            var items = new List<Material>();
            foreach (var pair in quest.Reward.ItemMap.OrderBy(kv => kv.Key))
            {
                var row = materialItemSheet.OrderedList.First(itemRow => itemRow.Id == pair.Key);
                var item = ItemFactory.CreateMaterial(row);
                var map = inventory.AddItem(item, count: pair.Value);
                itemMap.Add(map);
                items.Add(item);
            }

            quest.IsPaidInAction = true;
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();
        }

        [Obsolete("Use UpdateFromQuestReward")]
        public void UpdateFromQuestReward2(Quest.Quest quest, MaterialItemSheet materialItemSheet)
        {
            var items = new List<Material>();
            foreach (var pair in quest.Reward.ItemMap.OrderBy(kv => kv.Key))
            {
                var row = materialItemSheet.OrderedList.First(itemRow => itemRow.Id == pair.Key);
                var item = ItemFactory.CreateMaterial(row);
                var map = inventory.AddItem2(item, count: pair.Value);
                itemMap.Add(map);
                items.Add(item);
            }

            quest.IsPaidInAction = true;
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();
        }

        public void UpdateQuestRewards(MaterialItemSheet materialItemSheet)
        {
            var completedQuests = questList
                .Where(quest => quest.Complete && !quest.IsPaidInAction)
                .ToList();
            // 완료되었지만 보상을 받지 않은 퀘스트를 return 문에서 Select 하지 않고 미리 저장하는 이유는
            // 지연된 실행에 의해, return 시점에서 이미 모든 퀘스트의 보상 처리가 완료된 상태에서
            // completed를 호출 시 where문의 predicate가 평가되어 컬렉션이 텅 비기 때문이다.
            var completedQuestIds = completedQuests.Select(quest => quest.Id).ToList();
            foreach (var quest in completedQuests)
            {
                UpdateFromQuestReward(quest, materialItemSheet);
            }

            questList.completedQuestIds = completedQuestIds;
        }

        [Obsolete("Use UpdateQuestRewards")]
        public void UpdateQuestRewards2(MaterialItemSheet materialItemSheet)
        {
            var completedQuests = questList
                .Where(quest => quest.Complete && !quest.IsPaidInAction)
                .ToList();
            // 완료되었지만 보상을 받지 않은 퀘스트를 return 문에서 Select 하지 않고 미리 저장하는 이유는
            // 지연된 실행에 의해, return 시점에서 이미 모든 퀘스트의 보상 처리가 완료된 상태에서
            // completed를 호출 시 where문의 predicate가 평가되어 컬렉션이 텅 비기 때문이다.
            var completedQuestIds = completedQuests.Select(quest => quest.Id).ToList();
            foreach (var quest in completedQuests)
            {
                UpdateFromQuestReward2(quest, materialItemSheet);
            }

            questList.completedQuestIds = completedQuestIds;
        }

        #endregion

        public int GetArmorId()
        {
            var armor = inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            return armor?.Id ?? GameConfig.DefaultAvatarArmorId;
        }

        public void ValidateEquipments(List<Guid> equipmentIds, long blockIndex)
        {
            var ringCount = 0;
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                var equipment = (Equipment) outNonFungibleItem;
                if (equipment.RequiredBlockIndex > blockIndex)
                {
                    throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                var requiredLevel = 0;
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                        break;
                    case ItemSubType.Armor:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                        break;
                    case ItemSubType.Belt:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                        break;
                    case ItemSubType.Necklace:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                        break;
                    case ItemSubType.Ring:
                        ringCount++;
                        requiredLevel = ringCount == 1
                            ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                            : ringCount == 2
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                : int.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"{equipment.ItemSubType} / invalid equipment type");
                }

                if (level < requiredLevel)
                {
                    throw new EquipmentSlotUnlockException($"{equipment.ItemSubType} / not enough level. required: {requiredLevel}");
                }
            }
        }

        public List<Equipment> ValidateEquipmentsV2(List<Guid> equipmentIds, long blockIndex)
        {
            var countMap = new Dictionary<ItemSubType, int>();
            var list = new List<Equipment>();
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                var equipment = (Equipment)outNonFungibleItem;
                if (equipment.RequiredBlockIndex > blockIndex)
                {
                    throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                var type = equipment.ItemSubType;
                if (!countMap.ContainsKey(type))
                {
                    countMap[type] = 0;
                }

                countMap[type] += 1;

                var requiredLevel = 0;
                var isSlotEnough = true;
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Weapon;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon : int.MaxValue;
                        break;
                    case ItemSubType.Armor:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Armor;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor : int.MaxValue;
                        break;
                    case ItemSubType.Belt:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Belt;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt : int.MaxValue;
                        break;
                    case ItemSubType.Necklace:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Necklace;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace : int.MaxValue;
                        break;
                    case ItemSubType.Ring:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Ring;
                        requiredLevel = countMap[ItemSubType.Ring] == 1
                            ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                            : countMap[ItemSubType.Ring] == 2
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                : int.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"{equipment.ItemSubType} / invalid equipment type");
                }

                if (!isSlotEnough)
                {
                    throw new DuplicateEquipmentException($"Equipment slot of {equipment.ItemSubType} is full, but tried to equip {equipment.Id}");
                }

                if (level < requiredLevel)
                {
                    throw new EquipmentSlotUnlockException($"{equipment.ItemSubType} / not enough level. required: {requiredLevel}");
                }

                list.Add(equipment);
            }

            return list;
        }

        public List<int> ValidateConsumable(List<Guid> consumableIds, long currentBlockIndex)
        {
            var list = new List<int>();
            for (var slotIndex = 0; slotIndex < consumableIds.Count; slotIndex++)
            {
                var consumableId = consumableIds[slotIndex];

                if (!inventory.TryGetNonFungibleItem(consumableId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                var equipment = (Consumable) outNonFungibleItem;
                if (equipment.RequiredBlockIndex > currentBlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                int requiredLevel;
                switch (slotIndex)
                {
                    case 0:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot1;
                        break;
                    case 1:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot2;
                        break;
                    case 2:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot3;
                        break;
                    case 3:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot4;
                        break;
                    case 4:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot5;
                        break;
                    default:
                        throw new ConsumableSlotOutOfRangeException();
                }

                if (level < requiredLevel)
                {
                    throw new ConsumableSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(equipment.Id);
            }

            return list;
        }

        public List<int> ValidateCostume(IEnumerable<Guid> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
            var list = new List<int>();
            foreach (var costumeId in costumeIds)
            {
                if (!inventory.TryGetNonFungibleItem<Costume>(costumeId, out var costume))
                {
                    continue;
                }

                if (subTypes.Contains(costume.ItemSubType))
                {
                    throw new DuplicateCostumeException($"can't equip duplicate costume type : {costume.ItemSubType}");
                }

                subTypes.Add(costume.ItemSubType);

                int requiredLevel;
                switch (costume.ItemSubType)
                {
                    case ItemSubType.FullCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        break;
                    case ItemSubType.HairCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot;
                        break;
                    case ItemSubType.EarCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot;
                        break;
                    case ItemSubType.EyeCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot;
                        break;
                    case ItemSubType.TailCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot;
                        break;
                    case ItemSubType.Title:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                        break;
                    default:
                        throw new InvalidItemTypeException(
                            $"Costume[id: {costumeId}] isn't expected type. [type: {costume.ItemSubType}]");
                }

                if (level < requiredLevel)
                {
                    throw new CostumeSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(costume.Id);
            }

            return list;
        }

        public void ValidateCostume(HashSet<int> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
            foreach (var costumeId in costumeIds.OrderBy(i => i))
            {
#pragma warning disable 618
                if (!inventory.TryGetCostume(costumeId, out var costume))
#pragma warning restore 618
                {
                    continue;
                }

                if (subTypes.Contains(costume.ItemSubType))
                {
                    throw new DuplicateCostumeException($"can't equip duplicate costume type : {costume.ItemSubType}");
                }
                subTypes.Add(costume.ItemSubType);

                int requiredLevel;
                switch (costume.ItemSubType)
                {
                    case ItemSubType.FullCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        break;
                    case ItemSubType.HairCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot;
                        break;
                    case ItemSubType.EarCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot;
                        break;
                    case ItemSubType.EyeCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot;
                        break;
                    case ItemSubType.TailCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot;
                        break;
                    case ItemSubType.Title:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                        break;
                    default:
                        throw new InvalidItemTypeException(
                            $"Costume[id: {costumeId}] isn't expected type. [type: {costume.ItemSubType}]");
                }

                if (level < requiredLevel)
                {
                    throw new CostumeSlotUnlockException($"not enough level. required: {requiredLevel}");
                }
            }
        }

        public void ValidateItemRequirement(
            List<int> itemIds,
            List<Equipment> equipments,
            ItemRequirementSheet requirementSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet,
            string addressesHex)
        {
            foreach (var id in itemIds)
            {
                if (!requirementSheet.TryGetValue(id, out var requirementRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(ItemRequirementSheet), id);
                }

                if (level < requirementRow.Level)
                {
                    throw new NotEnoughAvatarLevelException(id, false, requirementRow.Level, level);
                }
            }

            foreach (var equipment in equipments)
            {
                if (!requirementSheet.TryGetValue(equipment.Id, out var requirementRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(ItemRequirementSheet), equipment.Id);
                }

                var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                    recipeSheet,
                    subRecipeSheet,
                    itemOptionSheet
                );
                var requirementLevel = isMadeWithMimisbrunnrRecipe
                    ? requirementRow.MimisLevel
                    : requirementRow.Level;
                if (level < requirementLevel)
                {
                    throw new NotEnoughAvatarLevelException(equipment.Id, isMadeWithMimisbrunnrRecipe, requirementLevel, level);
                }
            }
        }

        public void EquipItems(IEnumerable<Guid> itemIds)
        {
            // Unequip items already equipped.
            var equippableItems = inventory.Items
                .Select(item => item.item)
                .OfType<IEquippableItem>()
                .Where(equippableItem => equippableItem.Equipped);
#pragma warning disable LAA1002
            foreach (var equippableItem in equippableItems)
#pragma warning restore LAA1002
            {
                equippableItem.Unequip();
            }

            // Equip items.
            foreach (var itemId in itemIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out var inventoryItem) ||
                    !(inventoryItem.item is IEquippableItem equippableItem))
                {
                    continue;
                }

                equippableItem.Equip();
            }
        }

        // FIXME: Use `EquipItems(IEnumerable<Guid>)` instead of this.
        public void EquipCostumes(HashSet<int> costumeIds)
        {
            // 코스튬 해제.
            var inventoryCostumes = inventory.Items
                .Select(i => i.item)
                .OfType<Costume>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
#pragma warning disable LAA1002
            foreach (var costume in inventoryCostumes)
#pragma warning restore LAA1002
            {
                // FIXME: Use `costume.Unequip()`
                costume.equipped = false;
            }

            // 코스튬 장착.
            foreach (var costumeId in costumeIds.OrderBy(i => i))
            {
#pragma warning disable 618
                if (!inventory.TryGetCostume(costumeId, out var costume))
#pragma warning restore 618
                {
                    continue;
                }

                // FIXME: Use `costume.Unequip()`
                costume.equipped = true;
            }
        }

        // FIXME: Use `EquipItems(IEnumerable<Guid>)` instead of this.
        public void EquipEquipments(List<Guid> equipmentIds)
        {
            // 장비 해제.
            var inventoryEquipments = inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
#pragma warning disable LAA1002
            foreach (var equipment in inventoryEquipments)
#pragma warning restore LAA1002
            {
                equipment.Unequip();
            }

            // 장비 장착.
            foreach (var equipmentId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }
        }

        public int GetRandomSeed()
        {
            var bytes = address.ToByteArray().Concat(BitConverter.GetBytes(Nonce)).ToArray();
            var hash = SHA256.Create().ComputeHash(bytes);
            Nonce++;
            return BitConverter.ToInt32(hash, 0);
        }

        public List<T> GetNonFungibleItems<T>(List<Guid> itemIds)
        {
            var items = new List<T>();
            foreach (var nonFungibleId in itemIds)
            {
                if (!inventory.TryGetNonFungibleItem(nonFungibleId, out var inventoryItem))
                {
                    continue;
                }

                if (inventoryItem.item is T item)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyNameKey] = (Text)name,
                [(Text)LegacyCharacterIdKey] = (Integer)characterId,
                [(Text)LegacyLevelKey] = (Integer)level,
                [(Text)ExpKey] = (Integer)exp,
                [(Text)LegacyInventoryKey] = inventory.Serialize(),
                [(Text)LegacyWorldInformationKey] = worldInformation.Serialize(),
                [(Text)LegacyUpdatedAtKey] = updatedAt.Serialize(),
                [(Text)LegacyAgentAddressKey] = agentAddress.Serialize(),
                [(Text)LegacyQuestListKey] = questList.Serialize(),
                [(Text)LegacyMailBoxKey] = mailBox.Serialize(),
                [(Text)LegacyBlockIndexKey] = (Integer)blockIndex,
                [(Text)LegacyDailyRewardReceivedIndexKey] = (Integer)dailyRewardReceivedIndex,
                [(Text)LegacyActionPointKey] = (Integer)actionPoint,
                [(Text)LegacyStageMapKey] = stageMap.Serialize(),
                [(Text)LegacyMonsterMapKey] = monsterMap.Serialize(),
                [(Text)LegacyItemMapKey] = itemMap.Serialize(),
                [(Text)LegacyEventMapKey] = eventMap.Serialize(),
                [(Text)LegacyHairKey] = (Integer)hair,
                [(Text)LensKey] = (Integer)lens,
                [(Text)LegacyEarKey] = (Integer)ear,
                [(Text)LegacyTailKey] = (Integer)tail,
                [(Text)LegacyCombinationSlotAddressesKey] = combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)LegacyNonceKey] = Nonce.Serialize(),
                [(Text)LegacyRankingMapAddressKey] = RankingMapAddress.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

        public override IValue SerializeV2() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)NameKey] = (Text)name,
                [(Text)CharacterIdKey] = (Integer)characterId,
                [(Text)LevelKey] = (Integer)level,
                [(Text)ExpKey] = (Integer)exp,
                [(Text)UpdatedAtKey] = updatedAt.Serialize(),
                [(Text)AgentAddressKey] = agentAddress.Serialize(),
                [(Text)MailBoxKey] = mailBox.Serialize(),
                [(Text)BlockIndexKey] = (Integer)blockIndex,
                [(Text)DailyRewardReceivedIndexKey] = (Integer)dailyRewardReceivedIndex,
                [(Text)ActionPointKey] = (Integer)actionPoint,
                [(Text)StageMapKey] = stageMap.Serialize(),
                [(Text)MonsterMapKey] = monsterMap.Serialize(),
                [(Text)ItemMapKey] = itemMap.Serialize(),
                [(Text)EventMapKey] = eventMap.Serialize(),
                [(Text)HairKey] = (Integer)hair,
                [(Text)LensKey] = (Integer)lens,
                [(Text)EarKey] = (Integer)ear,
                [(Text)TailKey] = (Integer)tail,
                [(Text)CombinationSlotAddressesKey] = combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)RankingMapAddressKey] = RankingMapAddress.Serialize(),
            }.Union((Dictionary)base.SerializeV2()));
#pragma warning restore LAA1002
    }
}

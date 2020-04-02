using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.TableData;

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
        public DateTimeOffset updatedAt;
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

        public static Address CreateAvatarAddress()
        {
            var key = new PrivateKey();
            return key.PublicKey.ToAddress();
        }

        public AvatarState(
            Address address,
            Address agentAddress,
            long blockIndex,
            TableSheets sheets,
            string name = null) : base(address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            this.name = name ?? string.Empty;
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Inventory();
            worldInformation = new WorldInformation(blockIndex, sheets.WorldSheet, GameConfig.IsEditor);
            updatedAt = DateTimeOffset.UtcNow;
            this.agentAddress = agentAddress;
            questList = new QuestList(
                sheets.QuestSheet,
                sheets.QuestRewardSheet,
                sheets.QuestItemRewardSheet
            );
            mailBox = new MailBox();
            this.blockIndex = blockIndex;
            actionPoint = GameConfig.ActionPointMax;
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

            PostConstructor();
        }

        public AvatarState(Dictionary serialized)
            : base(serialized)
        {
            name = ((Text)serialized["name"]).Value;
            characterId = (int)((Integer)serialized["characterId"]).Value;
            level = (int)((Integer)serialized["level"]).Value;
            exp = (long)((Integer)serialized["exp"]).Value;
            inventory = new Inventory((List)serialized["inventory"]);
            worldInformation = new WorldInformation((Dictionary)serialized["worldInformation"]);
            updatedAt = serialized["updatedAt"].ToDateTimeOffset();
            agentAddress = new Address(((Binary)serialized["agentAddress"]).Value);
            questList = new QuestList((Dictionary) serialized["questList"]);
            mailBox = new MailBox((List)serialized["mailBox"]);
            blockIndex = (long)((Integer)serialized["blockIndex"]).Value;
            dailyRewardReceivedIndex = (long)((Integer)serialized["dailyRewardReceivedIndex"]).Value;
            actionPoint = (int)((Integer)serialized["actionPoint"]).Value;
            stageMap = new CollectionMap((Dictionary)serialized["stageMap"]);
            serialized.TryGetValue((Text)"monsterMap", out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Dictionary)value2);
            itemMap = new CollectionMap((Dictionary)serialized["itemMap"]);
            eventMap = new CollectionMap((Dictionary)serialized["eventMap"]);
            hair = (int)((Integer)serialized["hair"]).Value;
            lens = (int)((Integer)serialized["lens"]).Value;
            ear = (int)((Integer)serialized["ear"]).Value;
            tail = (int)((Integer)serialized["tail"]).Value;
            combinationSlotAddresses = serialized["combinationSlotAddresses"].ToList(StateExtensions.ToAddress);
            PostConstructor();
        }

        private void PostConstructor()
        {
            NameWithHash = $"{name} <size=80%><color=#A68F7E>#{address.ToHex().Substring(0, 4)}</color></size>";
        }

        public void Update(StageSimulator stageSimulator)
        {
            var player = stageSimulator.Player;
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldInformation = player.worldInformation;
            foreach (var pair in player.monsterMap)
            {
                monsterMap.Add(pair);
            }

            foreach (var pair in player.eventMap)
            {
                eventMap.Add(pair);
            }

            if (stageSimulator.Result == BattleLog.Result.Win)
            {
                stageMap.Add(new KeyValuePair<int, int>(stageSimulator.StageId, 1));
            }

            foreach (var pair in stageSimulator.ItemMap)
            {
                itemMap.Add(pair);
            }

            UpdateStageQuest(stageSimulator.Rewards);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Mail.Mail mail)
        {
            mailBox.Add(mail);
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

        public void UpdateFromRapidCombination(CombinationConsumable.ResultModel result,
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
            var type = itemUsable.Data.ItemType == ItemType.Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem(itemUsable, false);
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

        public void UpdateFromQuestReward(Quest.Quest quest, IActionContext context)
        {
            var items = new List<Material>();
            foreach (var pair in quest.Reward.ItemMap)
            {
                var row = TableSheets.FromActionContext(context)
                    .MaterialItemSheet.Values.First(itemRow => itemRow.Id == pair.Key);
                var item = ItemFactory.CreateMaterial(row);
                var map = inventory.AddItem(item, pair.Value);
                itemMap.Add(map);
                items.Add(item);
            }

            quest.IsPaidInAction = true;
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();
        }

        /// <summary>
        /// 완료된 퀘스트의 보상 처리를 한다.
        /// </summary>
        public void UpdateQuestRewards(IActionContext context)
        {
            var completedQuests = questList.Where(quest => quest.Complete && !quest.IsPaidInAction);
            // 완료되었지만 보상을 받지 않은 퀘스트를 return 문에서 Select 하지 않고 미리 저장하는 이유는
            // 지연된 실행에 의해, return 시점에서 이미 모든 퀘스트의 보상 처리가 완료된 상태에서
            // completed를 호출 시 where문의 predicate가 평가되어 컬렉션이 텅 비기 때문이다.
            var completedQuestIds = completedQuests.Select(quest => quest.Id).ToList();
            foreach (var quest in completedQuests)
            {
                UpdateFromQuestReward(quest, context);
            }

            questList.completedQuestIds = completedQuestIds;
        }

        #endregion

        public int GetArmorId()
        {
            var armor = inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            return armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"name"] = (Text)name,
                [(Text)"characterId"] = (Integer)characterId,
                [(Text)"level"] = (Integer)level,
                [(Text)"exp"] = (Integer)exp,
                [(Text)"inventory"] = inventory.Serialize(),
                [(Text)"worldInformation"] = worldInformation.Serialize(),
                [(Text)"updatedAt"] = updatedAt.Serialize(),
                [(Text)"agentAddress"] = agentAddress.Serialize(),
                [(Text)"questList"] = questList.Serialize(),
                [(Text)"mailBox"] = mailBox.Serialize(),
                [(Text)"blockIndex"] = (Integer)blockIndex,
                [(Text)"dailyRewardReceivedIndex"] = (Integer)dailyRewardReceivedIndex,
                [(Text)"actionPoint"] = (Integer)actionPoint,
                [(Text)"stageMap"] = stageMap.Serialize(),
                [(Text)"monsterMap"] = monsterMap.Serialize(),
                [(Text)"itemMap"] = itemMap.Serialize(),
                [(Text)"eventMap"] = eventMap.Serialize(),
                [(Text)"hair"] = (Integer)hair,
                [(Text)"lens"] = (Integer)lens,
                [(Text)"ear"] = (Integer)ear,
                [(Text)"tail"] = (Integer)tail,
                [(Text)"combinationSlotAddresses"] = combinationSlotAddresses.Select(i => i.Serialize()).Serialize(),
            }.Union((Dictionary)base.Serialize()));
    }
}

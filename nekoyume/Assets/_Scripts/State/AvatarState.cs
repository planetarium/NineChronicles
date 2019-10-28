using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Game.Quest;
using Nekoyume.Model;

namespace Nekoyume.State
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
        public Game.Item.Inventory inventory;
        public int worldStage;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address agentAddress;
        public QuestList questList;
        public MailBox mailBox;
        public long BlockIndex;
        public long nextDailyRewardIndex;
        public int actionPoint;
        public CollectionMap stageMap;
        public CollectionMap monsterMap;
        public CollectionMap itemMap;
        public CollectionMap eventMap;

        public AvatarState(Address address, Address agentAddress, long blockIndex, long rewardIndex, string name = null) : base(address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));                
            }
            
            this.name = name ?? "";
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Game.Item.Inventory();
            worldStage = 1;
            updatedAt = DateTimeOffset.UtcNow;
            this.agentAddress = agentAddress;
            questList = new QuestList();
            mailBox = new MailBox();
            BlockIndex = blockIndex;
            actionPoint = GameConfig.ActionPoint;
            nextDailyRewardIndex = rewardIndex;
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
            UpdateGeneralQuest(new []{createEvent, levelEvent});
            UpdateCompletedQuest();
        }
        
        public AvatarState(AvatarState avatarState) : base(avatarState.address)
        {
            if (avatarState == null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }
            
            name = avatarState.name;
            characterId = avatarState.characterId;
            level = avatarState.level;
            exp = avatarState.exp;
            inventory = avatarState.inventory;
            worldStage = avatarState.worldStage;
            updatedAt = avatarState.updatedAt;
            clearedAt = avatarState.clearedAt;
            agentAddress = avatarState.agentAddress;
        }

        public AvatarState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            name = ((Text) serialized[(Text) "name"]).Value;
            characterId = (int) ((Integer) serialized[(Text) "characterId"]).Value;
            level = (int) ((Integer) serialized[(Text) "level"]).Value;
            exp = (long) ((Integer) serialized[(Text) "exp"]).Value;
            inventory = new Game.Item.Inventory((Bencodex.Types.List) serialized[(Text) "inventory"]);
            worldStage = (int) ((Integer) serialized[(Text) "worldStage"]).Value;
            updatedAt = serialized[(Text) "updatedAt"].ToDateTimeOffset();
            clearedAt = serialized[(Text) "clearedAt"].ToNullableDateTimeOffset();
            agentAddress = new Address(((Binary) serialized[(Text) "agentAddress"]).Value);
            questList = new QuestList((Bencodex.Types.List) serialized[(Text) "questList"]);
            mailBox = new MailBox((Bencodex.Types.List) serialized[(Text) "mailBox"]);
            BlockIndex = (long) ((Integer) serialized[(Text) "blockIndex"]).Value;
            nextDailyRewardIndex = (long) ((Integer) serialized[(Text) "nextDailyRewardIndex"]).Value;
            actionPoint = (int) ((Integer) serialized[(Text) "actionPoint"]).Value;
            stageMap = new CollectionMap((Bencodex.Types.Dictionary) serialized[(Text) "stageMap"]);
            serialized.TryGetValue((Text) "monsterMap", out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Bencodex.Types.Dictionary) value2);
            itemMap = new CollectionMap((Bencodex.Types.Dictionary) serialized[(Text) "itemMap"]);
            eventMap = new CollectionMap((Bencodex.Types.Dictionary) serialized[(Text) "eventMap"]);
        }

        public void Update(Simulator simulator)
        {
            var player = simulator.Player;
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldStage = player.worldStage;
            foreach (var pair in player.monsterMap)
            {
                monsterMap.Add(pair);
            }
            foreach (var pair in player.eventMap)
            {
                eventMap.Add(pair);
            }
            if (simulator.Result == BattleLog.Result.Win)
            {
                stageMap.Add(new KeyValuePair<int, int>(simulator.WorldStage, 1));
            }
            foreach (var pair in simulator.ItemMap)
            {
                itemMap.Add(pair);
            }

            UpdateStageQuest(simulator.rewards);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Game.Mail.Mail mail)
        {
            mailBox.Add(mail);
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
            UpdateGeneralQuest(new []{QuestEventType.Level, QuestEventType.Die});
            UpdateCompletedQuest();
        }

        public void UpdateCombinationQuest(ItemUsable itemUsable)
        {
            questList.UpdateCombinationQuest(itemUsable);
            questList.UpdateItemTypeCollectQuest(new []{itemUsable});
            var type = itemUsable is Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int) type, 1));
            UpdateGeneralQuest(new[] {type});
            UpdateCompletedQuest();
        }
        public void UpdateItemEnhancementQuest(Equipment equipment)
        {
            questList.UpdateItemEnhancementQuest(equipment);
            var type = QuestEventType.Enhancement;
            eventMap.Add(new KeyValuePair<int, int>((int) type, 1));
            UpdateGeneralQuest(new[] {type});
            UpdateCompletedQuest();
        }

        public void UpdateQuestFromAddItem(ItemUsable itemUsable)
        {
            if (!itemMap.ContainsKey(itemUsable.Data.Id))
            {
                itemMap.Add(inventory.AddItem(itemUsable));
            }
            questList.UpdateItemGradeQuest(itemUsable);
            questList.UpdateItemTypeCollectQuest(new []{itemUsable});
            UpdateCompletedQuest();

        }
        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "name"] = (Text) name,
                [(Text) "characterId"] = (Integer) characterId,
                [(Text) "level"] = (Integer) level,
                [(Text) "exp"] = (Integer) exp,
                [(Text) "inventory"] = inventory.Serialize(),
                [(Text) "worldStage"] = (Integer) worldStage,
                [(Text) "updatedAt"] = updatedAt.Serialize(),
                [(Text) "clearedAt"] = clearedAt.Serialize(),
                [(Text) "agentAddress"] = agentAddress.Serialize(),
                [(Text) "questList"] = questList.Serialize(),
                [(Text) "mailBox"] = mailBox.Serialize(),
                [(Text) "blockIndex"] = (Integer) BlockIndex,
                [(Text) "nextDailyRewardIndex"] = (Integer) nextDailyRewardIndex,
                [(Text) "actionPoint"] = (Integer) actionPoint,
                [(Text) "stageMap"] = stageMap.Serialize(),
                [(Text) "monsterMap"] = monsterMap.Serialize(),
                [(Text) "itemMap"] = itemMap.Serialize(),
                [(Text) "eventMap"] = eventMap.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}

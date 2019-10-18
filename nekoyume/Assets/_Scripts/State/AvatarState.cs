using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
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
        public Inventory inventory;
        public int worldStage;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address agentAddress;
        public QuestList questList;
        public MailBox mailBox;
        public long BlockIndex;
        public long nextDailyRewardIndex;
        public int actionPoint;

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
            inventory = new Inventory();
            worldStage = 1;
            updatedAt = DateTimeOffset.UtcNow;
            this.agentAddress = agentAddress;
            questList = new QuestList();
            mailBox = new MailBox();
            BlockIndex = blockIndex;
            actionPoint = GameConfig.ActionPoint;
            nextDailyRewardIndex = rewardIndex;
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
            inventory = new Inventory((Bencodex.Types.List) serialized[(Text) "inventory"]);
            worldStage = (int) ((Integer) serialized[(Text) "worldStage"]).Value;
            updatedAt = serialized[(Text) "updatedAt"].ToDateTimeOffset();
            clearedAt = serialized[(Text) "clearedAt"].ToNullableDateTimeOffset();
            agentAddress = new Address(((Binary) serialized[(Text) "agentAddress"]).Value);
            questList = new QuestList((Bencodex.Types.List) serialized[(Text) "questList"]);
            mailBox = new MailBox((Bencodex.Types.List) serialized[(Text) "mailBox"]);
            BlockIndex = (long) ((Integer) serialized[(Text) "blockIndex"]).Value;
            nextDailyRewardIndex = (long) ((Integer) serialized[(Text) "nextDailyRewardIndex"]).Value;
            actionPoint = (int) ((Integer) serialized[(Text) "actionPoint"]).Value;
        }

        public void Update(Player player, List<ItemBase> items)
        {
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldStage = player.worldStage;
            questList.UpdateStageQuest(player, items);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Mail mail)
        {
            mailBox.Add(mail);
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
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}

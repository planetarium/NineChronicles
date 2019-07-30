using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;
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
        public BattleLog battleLog;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address agentAddress;
        public QuestList questList;

        public AvatarState(Address address, Address agentAddress, string name = null) : base(address)
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
            battleLog = null;
            updatedAt = DateTimeOffset.UtcNow;
            this.agentAddress = agentAddress;
            questList = new QuestList();
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
            battleLog = avatarState.battleLog;
            updatedAt = avatarState.updatedAt;
            clearedAt = avatarState.clearedAt;
            agentAddress = avatarState.agentAddress;
        }
        
        public void Update(Player player, List<ItemBase> items)
        {
            characterId = player.characterId;
            level = player.level;
            exp = player.exp;
            inventory = player.inventory;
            worldStage = player.worldStage;
            questList.UpdateStageQuest(player, items);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

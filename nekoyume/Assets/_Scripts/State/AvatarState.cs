using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState : State
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
        
        public AvatarState(Address address, string name = null) : base(address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));                
            }
            
            this.name = name ?? "";
            characterId = 100010;
            level = 1;
            exp = 0;
            inventory = new Inventory();
            worldStage = 1;
            battleLog = null;
            updatedAt = DateTimeOffset.UtcNow;
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
        }
        
        public void Update(Player player)
        {
            characterId = player.characterId;
            level = player.level;
            exp = player.exp;
            inventory = player.inventory;
            worldStage = player.worldStage;
        }
    }
}

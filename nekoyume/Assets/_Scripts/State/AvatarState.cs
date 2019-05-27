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
    public class AvatarState
    {
        public Address? AvatarAddress { get; }
        
        public Avatar avatar;
        
        public string name;
        public int level;
        public long exp;
        public int hpMax;
        public int currentHP;
        public List<Inventory.InventoryItem> items;
        public int worldStage;
        public int id;
        
        public BattleLog battleLog;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;

        public AvatarState(Avatar avatar, Address? address, BattleLog logs = null)
        {
            this.avatar = avatar;
            battleLog = logs;
            updatedAt = DateTimeOffset.UtcNow;
            AvatarAddress = address;
        }
        
        public AvatarState(Address address, AvatarState avatarState, BattleLog logs = null)
        {
            name = avatarState.name;
            level = avatarState.level;
            exp = avatarState.exp;
            hpMax = avatarState.hpMax;
            currentHP = avatarState.currentHP;
            items = avatarState.items;
            worldStage = avatarState.worldStage;
            id = avatarState.id;
            
            battleLog = logs;
            updatedAt = DateTimeOffset.UtcNow;
            AvatarAddress = address;
        }
        
        public void Update(Player player)
        {
            level = player.level;
            exp = player.exp;
            hpMax = player.hp;
            currentHP = hpMax;
            items = player.Items;
            worldStage = player.stage;
            id = player.job;
        }

        public Player ToPlayer()
        {
            return new Player(this);
        }

        public void AddEquipmentItemToItems(int itemId, int count)
        {
            if (!Tables.instance.TryGetItemEquipment(itemId, out var itemData))
            {
                throw new KeyNotFoundException($"itemId: {itemId}");
            }
            
            var inventoryItem = items.FirstOrDefault(item => item.Item.Data.id == itemId);
            if (ReferenceEquals(inventoryItem, null))
            {
                var itemBase = ItemBase.ItemFactory(itemData);
                items.Add(new Inventory.InventoryItem(itemBase, count));
            }
            else
            {
                inventoryItem.Count += count;
            }
        }

        public void RemoveEquipmentItemFromItems(int itemId, int count)
        {
            if (!Tables.instance.TryGetItemEquipment(itemId, out var itemData))
            {
                throw new KeyNotFoundException($"itemId: {itemId}");
            }
            
            var inventoryItem = items.FirstOrDefault(item => item.Item.Data.id == itemId);
            if (ReferenceEquals(inventoryItem, null))
            {
                throw new KeyNotFoundException($"itemId: {itemId}");
            }

            if (inventoryItem.Count < count)
            {
                throw new InvalidOperationException("Reduce more than the quantity of inventoryItem.");
            }
            
            inventoryItem.Count -= count;

            if (inventoryItem.Count == 0)
            {
                items.Remove(inventoryItem);
            }
        }
    }
}

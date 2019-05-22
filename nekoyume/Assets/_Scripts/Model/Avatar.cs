using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Avatar
    {
        protected bool Equals(Avatar other)
        {
            return string.Equals(Name, other.Name) && Level == other.Level && EXP == other.EXP &&
                   HPMax == other.HPMax && CurrentHP == other.CurrentHP && Equals(Items, other.Items) &&
                   WorldStage == other.WorldStage;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Avatar) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Level;
                hashCode = (hashCode * 397) ^ EXP.GetHashCode();
                hashCode = (hashCode * 397) ^ HPMax;
                hashCode = (hashCode * 397) ^ CurrentHP;
                hashCode = (hashCode * 397) ^ (Items != null ? Items.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ WorldStage;
                return hashCode;
            }
        }

        public string Name;
        public int Level;
        public long EXP;
        public int HPMax;
        public int CurrentHP;
        public List<Inventory.InventoryItem> Items;
        public int WorldStage;
        public int id;

        public void Update(Player player)
        {
            Level = player.level;
            EXP = player.exp;
            HPMax = player.hp;
            CurrentHP = HPMax;
            Items = player.Items;
            WorldStage = player.stage;
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
            
            var inventoryItem = Items.FirstOrDefault(item => item.Item.Data.id == itemId);
            if (ReferenceEquals(inventoryItem, null))
            {
                var itemBase = ItemBase.ItemFactory(itemData);
                Items.Add(new Inventory.InventoryItem(itemBase, count));
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
            
            var inventoryItem = Items.FirstOrDefault(item => item.Item.Data.id == itemId);
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
                Items.Remove(inventoryItem);
            }
        }
    }
}

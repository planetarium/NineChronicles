using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    [Serializable]
    public class Avatar
    {
        protected bool Equals(Avatar other)
        {
            return string.Equals(Name, other.Name) && Level == other.Level && EXP == other.EXP && HPMax == other.HPMax && CurrentHP == other.CurrentHP && Equals(Items, other.Items) && WorldStage == other.WorldStage && Dead == other.Dead;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
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
                hashCode = (hashCode * 397) ^ Dead.GetHashCode();
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
        public bool Dead = false;

        public void Update(Player player)
        {
            Level = player.level;
            EXP = player.exp;
            HPMax = player.hpMax;
            CurrentHP = player.hp;
            Items = player.Items;
            WorldStage = player.stage;
            Dead = player.isDead;
        }
    }
}

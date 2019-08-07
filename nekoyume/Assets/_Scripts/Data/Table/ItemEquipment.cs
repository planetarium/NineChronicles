using System;
using Assets.SimpleLocalization;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class ItemEquipment : Item
    {
        protected bool Equals(ItemEquipment other)
        {
            return id == other.id && string.Equals(name, other.name) && elemental == other.elemental &&
                   grade == other.grade && setId == other.setId && string.Equals(ability1, other.ability1) &&
                   value1 == other.value1 && string.Equals(ability2, other.ability2) && value2 == other.value2 &&
                   resourceId == other.resourceId &&
                   string.Equals(description, other.description);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemEquipment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ resourceId;
                hashCode = (hashCode * 397) ^ (int) elemental;
                hashCode = (hashCode * 397) ^ setId;
                hashCode = (hashCode * 397) ^ (ability1 != null ? ability1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ value1;
                hashCode = (hashCode * 397) ^ (ability2 != null ? ability2.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ value2;
                hashCode = (hashCode * 397) ^ turnSpeed.GetHashCode();
                hashCode = (hashCode * 397) ^ attackRange.GetHashCode();
                return hashCode;
            }
        }

        public int resourceId = 0;
        public Elemental.ElementalType elemental;
        public int setId = 0;
        public string ability1 = "";
        public int value1 = 0;
        public string ability2 = "";
        public int value2 = 0;
        public decimal turnSpeed = 2.0m;
        public decimal attackRange = 1.0m;
        public int skillId = 0;
        public float skillChance = 0.0f;
        
        public override string LocalizedName => LocalizationManager.LocalizeEquipmentName(id);
        public override string LocalizedDescription => LocalizationManager.LocalizeEquipmentDescription(id);
    }
}

using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Item : Row
    {
        protected bool Equals(Item other)
        {
            return id == other.id && string.Equals(cls, other.cls) && param0 == other.param0 &&
                   param1 == other.param1 && param2 == other.param2;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Item) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = id;
                hashCode = (hashCode * 397) ^ (cls != null ? cls.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ param0;
                hashCode = (hashCode * 397) ^ param1;
                hashCode = (hashCode * 397) ^ param2;
                return hashCode;
            }
        }

        public int id = 0;
        public string cls = "";
        public int param0 = 0;
        public int param1 = 0;
        public int param2 = 0;
        public int Synergy = 0;
        public Elemental.ElementalType elemental;
        public string name = "";
        public string description = "";
    }
}

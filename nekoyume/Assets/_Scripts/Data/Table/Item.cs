using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Item : Row
    {
        protected bool Equals(Item other)
        {
            return Id == other.Id && string.Equals(Cls, other.Cls) && param0 == other.param0 &&
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
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Cls != null ? Cls.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ param0;
                hashCode = (hashCode * 397) ^ param1;
                hashCode = (hashCode * 397) ^ param2;
                return hashCode;
            }
        }

        public int Id = 0;
        public string Cls = "";
        public int param0 = 0;
        public int param1 = 0;
        public int param2 = 0;
        public int Synergy = 0;
        public string Name = "";
        public string Flavour = "";
    }
}

using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Item : Row
    {
        protected bool Equals(Item other)
        {
            return Id == other.Id && string.Equals(Cls, other.Cls) && Param_0 == other.Param_0 &&
                   Param_1 == other.Param_1 && Param_2 == other.Param_2;
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
                hashCode = (hashCode * 397) ^ Param_0;
                hashCode = (hashCode * 397) ^ Param_1;
                hashCode = (hashCode * 397) ^ Param_2;
                return hashCode;
            }
        }

        public int Id = 0;
        public string Cls = "";
        public int Param_0 = 0;
        public int Param_1 = 0;
        public int Param_2 = 0;
        public string Name = "";
        public string Flavour = "";
    }
}

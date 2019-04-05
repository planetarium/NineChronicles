using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Item : Row
    {
        protected bool Equals(Item other)
        {
            return id == other.id && string.Equals(cls, other.cls) && string.Equals(name, other.name) &&
                   grade == other.grade && string.Equals(description, other.description);
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
                hashCode = (hashCode * 397) ^ (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ grade;
                hashCode = (hashCode * 397) ^ (description != null ? description.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int id = 0;
        public string cls = "";
        public string name = "";
        public int grade = 0;
        public string description = "";
    }
}

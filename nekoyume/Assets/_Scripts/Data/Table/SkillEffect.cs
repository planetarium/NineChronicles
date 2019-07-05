using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class SkillEffect : Row
    {
        public enum SkillType
        {
            Attack,
            Buff,
            Debuff,
        }

        public enum Target
        {
            Enemy,
            Enemies,
            Self,
            Ally,
        }

        public enum Category
        {
            Normal,
            Double,
            Area,
            Blow,
        }

        public int id = 0;
        public SkillType type = SkillType.Attack;
        public Category category = Category.Normal;
        public Target target = Target.Enemy;
        public int hitCount = 1;

        protected bool Equals(SkillEffect other)
        {
            return id == other.id && type == other.type && category == other.category && target == other.target &&
                   hitCount == other.hitCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SkillEffect) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = id;
                hashCode = (hashCode * 397) ^ (int) type;
                hashCode = (hashCode * 397) ^ (int) category;
                hashCode = (hashCode * 397) ^ (int) target;
                hashCode = (hashCode * 397) ^ hitCount;
                return hashCode;
            }
        }
    }
}

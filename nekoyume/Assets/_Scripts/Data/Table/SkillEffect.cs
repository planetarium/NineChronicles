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

        public SkillType type = SkillType.Attack;
        public Category category = Category.Normal;
        public Target target = Target.Enemy;
        public float multiplier = 1.0f;
        public int hitCount = 1;

        protected bool Equals(SkillEffect other)
        {
            return type == other.type &&
                   category == other.category &&
                   target == other.target &&
                   multiplier.Equals(other.multiplier) &&
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
                var hashCode = (int) type;
                hashCode = (hashCode * 397) ^ (int) category;
                hashCode = (hashCode * 397) ^ (int) target;
                hashCode = (hashCode * 397) ^ multiplier.GetHashCode();
                hashCode = (hashCode * 397) ^ hitCount;
                return hashCode;
            }
        }
    }
}

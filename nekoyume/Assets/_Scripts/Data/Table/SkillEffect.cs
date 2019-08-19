using System;
using Nekoyume.EnumType;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class SkillEffect : Row
    {
        public int id = 0;
        public SkillType skillType = SkillType.Attack;
        public SkillCategory skillCategory = SkillCategory.Normal;
        public SkillTargetType skillTargetType = SkillTargetType.Enemy;
        public int hitCount = 1;

        protected bool Equals(SkillEffect other)
        {
            return id == other.id && skillType == other.skillType && skillCategory == other.skillCategory && skillTargetType == other.skillTargetType &&
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
                hashCode = (hashCode * 397) ^ (int) skillType;
                hashCode = (hashCode * 397) ^ (int) skillCategory;
                hashCode = (hashCode * 397) ^ (int) skillTargetType;
                hashCode = (hashCode * 397) ^ hitCount;
                return hashCode;
            }
        }
    }
}

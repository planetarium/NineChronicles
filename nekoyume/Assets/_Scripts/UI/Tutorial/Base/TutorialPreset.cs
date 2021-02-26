using System;

namespace Nekoyume.UI
{
    [Serializable]
    public class TutorialPreset
    {
        public Preset[] preset { get; set; }
    }

    [Serializable]
    public class Preset
    {
        public int id { get; set; }

        public string content { get; set; }

        public bool isExistFadeInBackground { get; set; }

        public bool isEnableMask { get; set; }

        public bool isSkipArrowAnimation { get; set; }

        public int commaId { get; set; }

        protected bool Equals(Preset other)
        {
            return id == other.id &&
                   content == other.content &&
                   isExistFadeInBackground == other.isExistFadeInBackground &&
                   isEnableMask == other.isEnableMask &&
                   isSkipArrowAnimation == other.isSkipArrowAnimation &&
                   commaId == other.commaId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Preset) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = id;
                hashCode = (hashCode * 397) ^ (content != null ? content.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isExistFadeInBackground.GetHashCode();
                hashCode = (hashCode * 397) ^ isEnableMask.GetHashCode();
                hashCode = (hashCode * 397) ^ isSkipArrowAnimation.GetHashCode();
                hashCode = (hashCode * 397) ^ commaId;
                return hashCode;
            }
        }
    }
}

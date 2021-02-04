using System;
using System.Text.Json.Serialization;

namespace Nekoyume.UI
{
    [Serializable]
    public class TutorialScenario
    {
        public Scenario[] scenario { get; set; }
    }

    [Serializable]
    public class Scenario
    {
        public int id { get; set; }

        public int nextId { get; set; }

        public ScenarioData data { get; set; }

        protected bool Equals(Scenario other)
        {
            return id == other.id &&
                   nextId == other.nextId &&
                   Equals(data, other.data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Scenario) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = id;
                hashCode = (hashCode * 397) ^ nextId;
                hashCode = (hashCode * 397) ^ (data != null ? data.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Serializable]
    public class ScenarioData
    {
        public int presetId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TutorialTargetType targetType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TutorialActionType actionType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GuideType guideType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DialogEmojiType emojiType { get; set; }

        public string scriptKey { get; set; }

        protected bool Equals(ScenarioData other)
        {
            return presetId == other.presetId &&
                   targetType == other.targetType &&
                   actionType == other.actionType &&
                   guideType == other.guideType &&
                   emojiType == other.emojiType &&
                   scriptKey == other.scriptKey;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScenarioData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = presetId;
                hashCode = (hashCode * 397) ^ (int) targetType;
                hashCode = (hashCode * 397) ^ (int) actionType;
                hashCode = (hashCode * 397) ^ (int) guideType;
                hashCode = (hashCode * 397) ^ (int) emojiType;
                hashCode = (hashCode * 397) ^ (scriptKey != null ? scriptKey.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}

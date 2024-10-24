using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;

namespace Nekoyume.UI.Model
{
    public class InitializeResponse
    {
        public AvatarModel Avatar { get; set; }
        public PolicyModel Policy { get; set; }
    }

    public class AvatarResponse
    {
        public AvatarModel Avatar { get; set; }
    }

    public class AvatarModel
    {
        public string AgentAddress { get; set; }
        public string AvatarAddress { get; set; }
        public string CreatedAt { get; set; }
        public string LastClaimedAt { get; set; }
        public int Level { get; set; }
    }

    public class PolicyResponse
    {
        public PolicyModel Policy { get; set; }
    }

    public class PolicyModel
    {
        public bool Activate { get; set; }
        public int MinimumLevel { get; set; }
        public int? MaxLevel { get; set; }

        [JsonConverter(typeof(TimespanConverter))]
        public TimeSpan MinimumRequiredInterval { get; set; }

        public List<PatrolRewardModel> Rewards { get; set; }
    }

    public class PatrolRewardModel
    {
        // FungibleAssetValueRewardModel
        public string Currency { get; set; }

        // FungibleItemRewardModel
        public string FungibleId { get; set; }
        public int? ItemId { get; set; }

        public int PerInterval { get; set; }

        [JsonConverter(typeof(TimespanConverter))]
        public TimeSpan RewardInterval { get; set; }
    }

    public class ClaimResponse
    {
        public string Claim { get; set; }
    }

    public class PutAvatarResponse
    {
        public AvatarModel PutAvatar { get; set; }
    }

    public class TimespanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(TimeSpan))
            {
                throw new ArgumentException();
            }

            var spanString = reader.Value as string;
            if (spanString == null)
            {
                return null;
            }

            return XmlConvert.ToTimeSpan(spanString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var duration = (TimeSpan)value;
            writer.WriteValue(XmlConvert.ToString(duration));
        }
    }
}

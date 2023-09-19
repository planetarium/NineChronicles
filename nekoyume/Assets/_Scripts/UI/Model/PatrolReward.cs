using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Nekoyume.GraphQL;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
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

    public class RewardPolicyModel
    {
        public bool Activate { get; set; }
        public int MinimumLevel { get; set; }
        public int? MaxLevel { get; set; }
        [JsonConverter(typeof(TimespanConverter))]
        public TimeSpan MinimumRequiredInterval { get; set; }
        public List<PatrolRewardModel> Rewards { get; set; }
    }

    public class RewardPolicyResponse
    {
        public RewardPolicyModel Policy { get; set; }
    }

    public class PatrolReward
    {
        public bool Initialized { get; private set; } = false;

        private int AvatarLevel { get; set; }
        public readonly ReactiveProperty<DateTime> LastRewardTime = new();
        public DateTime CreatedTime { get; private set; }

        public int NextLevel { get; private set; }
        public readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public async void Initialize()
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;

            await LoadAvatarInfo(serviceClient, avatarAddress.ToHex(), agentAddress.ToHex());
            await LoadPolicyInfo(serviceClient, level);

            Initialized = true;
        }

        public async void LoadPolicyInfo(int level)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            await LoadPolicyInfo(serviceClient, level);
        }

        public async void LoadAvatarInfo()
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            await LoadAvatarInfo(serviceClient, avatarAddress.ToHex(), agentAddress.ToHex());
        }

        public void ClaimReward()
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            // Todo : ClaimReward

            // LoadAvatarInfo();
            LastRewardTime.Value = DateTime.Now;
        }

        private async Task LoadAvatarInfo(
            NineChroniclesAPIClient apiClient, string avatarAddress, string agentAddress)
        {
            var query = $@"query {{
                avatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
                    avatarAddress
                    agentAddress
                    createdAt
                    lastClaimedAt
                    level
                }}
            }}";

            var response = await apiClient.GetObjectAsync<AvatarResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(AvatarResponse)}");
                return;
            }

            AvatarLevel = response.Avatar.Level;
            var lastClaimedAt = response.Avatar.LastClaimedAt ?? response.Avatar.CreatedAt;
            LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
            CreatedTime = DateTime.Parse(response.Avatar.CreatedAt);
        }

        private async Task LoadPolicyInfo(
            NineChroniclesAPIClient apiClient, int level, bool free = true)
        {
            var query = $@"query {{
                policy(level: {level}, free: true) {{
                    activate
                    minimumLevel
                    maxLevel
                    minimumRequiredInterval
                    rewards {{
                        ... on FungibleAssetValueRewardModel {{
                            currency
                            perInterval
                            rewardInterval
                        }}
                        ... on FungibleItemRewardModel {{
                            itemId
                            perInterval
                            rewardInterval
                        }}
                    }}
                }}
            }}";

            var response = await apiClient.GetObjectAsync<RewardPolicyResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(RewardPolicyResponse)}");
                return;
            }

            NextLevel = response.Policy.MaxLevel ?? int.MaxValue;
            RewardModels.Value = response.Policy.Rewards;
        }
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
                throw new ArgumentException();

            var spanString = reader.Value as string;
            if (spanString == null)
                return null;
            return XmlConvert.ToTimeSpan(spanString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var duration = (TimeSpan) value;
            writer.WriteValue(XmlConvert.ToString(duration));
        }
    }
}

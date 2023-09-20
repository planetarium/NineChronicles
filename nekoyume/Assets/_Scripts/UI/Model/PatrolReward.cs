using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.GraphQL;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model.Patrol
{
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

        public async void ClaimReward()
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            // Todo : ClaimReward
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            await ClaimReward(serviceClient, avatarAddress.ToHex(), agentAddress.ToHex());

            LoadAvatarInfo();
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

            var response = await apiClient.GetObjectAsync<PolicyResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(PolicyResponse)}");
                return;
            }

            NextLevel = response.Policy.MaxLevel ?? int.MaxValue;
            RewardModels.Value = response.Policy.Rewards;
        }

        private async Task ClaimReward(
            NineChroniclesAPIClient apiClient, string avatarAddress, string agentAddress)
        {
            var query = $@"mutation {{
                claim(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"")
            }}";

            var response = await apiClient.GetObjectAsync<ClaimResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(ClaimResponse)}");
                return;
            }

            Debug.Log($"Claimed tx : {response.Claim}");
        }
    }
}

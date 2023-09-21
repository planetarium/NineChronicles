using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;

            await LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
            await LoadPolicyInfo(level);

            Initialized = true;
        }

        public async void LoadPolicyInfo()
        {
            var level = Game.Game.instance.States.CurrentAvatarState.level;
            if (NextLevel <= level)
            {
                await LoadPolicyInfo(level);
            }
        }

        public async void LoadAvatarInfo()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            await LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
        }

        public async void ClaimReward()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            await ClaimReward(avatarAddress.ToHex(), agentAddress.ToHex());

            LoadAvatarInfo();
        }

        private async Task LoadAvatarInfo(string avatarAddress, string agentAddress)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var query = $@"query {{
                avatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
                    avatarAddress
                    agentAddress
                    createdAt
                    lastClaimedAt
                    level
                }}
            }}";

            var response = await serviceClient.GetObjectAsync<AvatarResponse>(query);
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

        private async Task LoadPolicyInfo(int level, bool free = true)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

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

            var response = await serviceClient.GetObjectAsync<PolicyResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(PolicyResponse)}");
                return;
            }

            NextLevel = response.Policy.MaxLevel ?? int.MaxValue;
            RewardModels.Value = response.Policy.Rewards;
        }

        private async Task ClaimReward(string avatarAddress, string agentAddress)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var query = $@"mutation {{
                claim(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"")
            }}";

            var response = await serviceClient.GetObjectAsync<ClaimResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(ClaimResponse)}");
                return;
            }

            Debug.Log($"Claimed tx : {response.Claim}");
        }
    }
}

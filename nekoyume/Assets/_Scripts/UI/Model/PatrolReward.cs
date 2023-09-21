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
        private readonly ReactiveProperty<DateTime> LastRewardTime = new();

        public int NextLevel { get; private set; }
        public TimeSpan Interval { get; private set; }
        public readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public IReadOnlyReactiveProperty<TimeSpan> PatrolTime;
        public IObservable<bool> CanClaim => PatrolTime?.Select(time => time >= Interval);

        public async Task Initialize()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;

            await LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
            await LoadPolicyInfo(level);

            PatrolTime = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
                .CombineLatest(LastRewardTime, (_, lastReward) =>
                {
                    var timeSpan = DateTime.Now - lastReward;
                    return timeSpan > Interval ? Interval : timeSpan;
                })
                .ToReactiveProperty();

            Initialized = true;
        }

        public async Task LoadAvatarInfo(string avatarAddress, string agentAddress)
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

            if (response.Avatar is null)
            {
                await PutAvatar(avatarAddress, agentAddress);
                return;
            }

            AvatarLevel = response.Avatar.Level;
            var lastClaimedAt = response.Avatar.LastClaimedAt ?? response.Avatar.CreatedAt;
            LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
        }

        public async Task LoadPolicyInfo(int level, bool free = true)
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
            Interval = response.Policy.MinimumRequiredInterval;
            RewardModels.Value = response.Policy.Rewards;
        }

        public async Task<string> ClaimReward(string avatarAddress, string agentAddress)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query = $@"mutation {{
                claim(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"")
            }}";

            var response = await serviceClient.GetObjectAsync<ClaimResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(ClaimResponse)}");
                return null;
            }

            return response.Claim;
        }

        private async Task PutAvatar(string avatarAddress, string agentAddress)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var query = $@"mutation {{
                putAvatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
                    avatarAddress
                    agentAddress
                    createdAt
                    lastClaimedAt
                    level
                }}
            }}";

            var response = await serviceClient.GetObjectAsync<PutAvatarResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(PutAvatarResponse)}");
                return;
            }

            AvatarLevel = response.PutAvatar.Level;
            var lastClaimedAt = response.PutAvatar.LastClaimedAt ?? response.PutAvatar.CreatedAt;
            LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
        }
    }
}

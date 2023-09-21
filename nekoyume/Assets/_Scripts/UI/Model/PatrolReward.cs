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

        private IObservable<long> Timer => Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        public IReadOnlyReactiveProperty<TimeSpan> PatrolTime => Timer
            .CombineLatest(LastRewardTime, (_, lastReward) =>
            {
                var timeSpan = DateTime.Now - lastReward;
                return timeSpan > Interval ? Interval : timeSpan;
            })
            .ToReactiveProperty();

        public IObservable<bool> CanClaim => PatrolTime.Select(time => time > Interval);

        public async Task Initialize()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;

            await LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
            await LoadPolicyInfo(level);

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

        public async Task ClaimReward(string avatarAddress, string agentAddress)
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

            Debug.LogError($"Claimed tx : {response.Claim}");
        }
    }
}

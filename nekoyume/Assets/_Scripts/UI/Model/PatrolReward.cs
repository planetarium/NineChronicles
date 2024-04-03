using Nekoyume.L10n;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.UI.Model.Patrol
{
    using UniRx;
    public class PatrolReward
    {
        private int AvatarLevel { get; set; }
        private readonly ReactiveProperty<DateTime> LastRewardTime = new();

        public int NextLevel { get; private set; }
        public TimeSpan Interval { get; private set; }
        public readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public IReadOnlyReactiveProperty<TimeSpan> PatrolTime;

        private const string PatrolRewardPushIdentifierKey = "PATROL_REWARD_PUSH_IDENTIFIER";
        public bool Initialized;

        public async Task InitializeInformation(string avatarAddress, string agentAddress, int level)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var query =
$@"query {{
    avatar(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"") {{
        avatarAddress
        agentAddress
        createdAt
        lastClaimedAt
        level
    }}
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

            var response = await serviceClient.GetObjectAsync<InitializeResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(InitializeResponse)}");
                return;
            }

            if (response.Avatar is null)
            {
                await PutAvatar(avatarAddress, agentAddress);
            }
            else
            {
                AvatarLevel = response.Avatar.Level;
                var lastClaimedAt = response.Avatar.LastClaimedAt ?? response.Avatar.CreatedAt;
                LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
            }

            if (response.Policy is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse.Policy)}");
            }
            else
            {
                NextLevel = response.Policy.MaxLevel ?? int.MaxValue;
                Interval = response.Policy.MinimumRequiredInterval;
                RewardModels.Value = response.Policy.Rewards;
            }

            if (Initialized)
            {
                return;
            }

            Initialized = true;
            PatrolTime = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
                .CombineLatest(LastRewardTime, (_, lastReward) =>
                {
                    var timeSpan = DateTime.Now - lastReward;
                    return timeSpan > Interval ? Interval : timeSpan;
                })
                .ToReactiveProperty();
            LastRewardTime.ObserveOnMainThread().Subscribe(_ => SetPushNotification());
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
                NcDebug.LogError($"Failed getting response : {nameof(AvatarResponse)}");
                return;
            }

            if (response.Avatar is null)
            {
                await PutAvatar(avatarAddress, agentAddress);
            }
            else
            {
                AvatarLevel = response.Avatar.Level;
                var lastClaimedAt = response.Avatar.LastClaimedAt ?? response.Avatar.CreatedAt;
                LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
            }
        }

        public async Task LoadPolicyInfo(int level, bool free = true)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return;
            }

            var query =
$@"query {{
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
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse)}");
                return;
            }

            if (response.Policy is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PolicyResponse.Policy)}");
            }
            else
            {
                NextLevel = response.Policy.MaxLevel ?? int.MaxValue;
                Interval = response.Policy.MinimumRequiredInterval;
                RewardModels.Value = response.Policy.Rewards;
            }
        }

        public async Task<string> ClaimReward(string avatarAddress, string agentAddress)
        {
            var serviceClient = Game.Game.instance.PatrolRewardServiceClient;
            if (!serviceClient.IsInitialized)
            {
                return null;
            }

            var query =
$@"mutation {{
    claim(avatarAddress: ""{avatarAddress}"", agentAddress: ""{agentAddress}"")
}}";

            var response = await serviceClient.GetObjectAsync<ClaimResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(ClaimResponse)}");
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

            var query =
$@"mutation {{
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
                NcDebug.LogError($"Failed getting response : {nameof(PutAvatarResponse)}");
                return;
            }

            if (response.PutAvatar is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(PutAvatarResponse.PutAvatar)}");
                return;
            }

            AvatarLevel = response.PutAvatar.Level;
            var lastClaimedAt = response.PutAvatar.LastClaimedAt ?? response.PutAvatar.CreatedAt;
            LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
        }

        private void SetPushNotification()
        {
            var prevPushIdentifier = PlayerPrefs.GetString(PatrolRewardPushIdentifierKey, string.Empty);
            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(PatrolRewardPushIdentifierKey);
            }

            var completeTime = LastRewardTime.Value + Interval - DateTime.Now;

            var pushIdentifier = PushNotifier.Push(L10nManager.Localize("PUSH_PATROL_REWARD_COMPLETE_CONTENT"), completeTime, PushNotifier.PushType.Reward);
            PlayerPrefs.SetString(PatrolRewardPushIdentifierKey, pushIdentifier);
        }
    }
}

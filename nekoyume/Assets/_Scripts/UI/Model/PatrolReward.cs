using Nekoyume.L10n;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.ApiClient
{
    using UniRx;

    public class PatrolReward
    {
        private readonly ReactiveProperty<DateTime> LastRewardTime = new();
        public int NextLevel { get; private set; }
        public TimeSpan Interval { get; private set; }
        public readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public IReadOnlyReactiveProperty<TimeSpan> PatrolTime;

        private const string PatrolRewardPushIdentifierKey = "PATROL_REWARD_PUSH_IDENTIFIER";
        public bool Initialized;

        // Called at CurrentAvatarState isNewlySelected
        public async Task InitializeInformation(string avatarAddress, string agentAddress, int level)
        {
            var (avatar, policy) =
                await PatrolRewardQuery.InitializeInformation(avatarAddress, agentAddress, level);
            if (policy is not null)
            {
                SetPolicyModel(policy);
            }

            if (avatar is not null)
            {
                SetAvatarModel(avatar);
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
            var avatar = await PatrolRewardQuery.LoadAvatarInfo(avatarAddress, agentAddress);
            if (avatar is not null)
            {
                SetAvatarModel(avatar);
            }
        }

        public async Task LoadPolicyInfo(int level, bool free = true)
        {
            var policy = await PatrolRewardQuery.LoadPolicyInfo(level, free);
            if (policy is not null)
            {
                SetPolicyModel(policy);
            }
        }

        public async Task<string> ClaimReward(string avatarAddress, string agentAddress)
        {
            return await PatrolRewardQuery.ClaimReward(avatarAddress, agentAddress);
        }

        private void SetAvatarModel(AvatarModel avatar)
        {
            var lastClaimedAt = avatar.LastClaimedAt ?? avatar.CreatedAt;
            LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
        }

        private void SetPolicyModel(PolicyModel policy)
        {
            NextLevel = policy.MaxLevel ?? int.MaxValue;
            Interval = policy.MinimumRequiredInterval;
            RewardModels.Value = policy.Rewards;
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

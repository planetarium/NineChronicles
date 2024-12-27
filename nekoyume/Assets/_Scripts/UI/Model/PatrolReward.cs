using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.GraphQL;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.ApiClient
{
    using UniRx;

    public static class PatrolReward
    {
        public static readonly ReactiveProperty<DateTime> LastRewardTime = new();
        public static int NextLevel { get; private set; }
        public static TimeSpan Interval { get; private set; }
        public static readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public static readonly IReadOnlyReactiveProperty<TimeSpan> PatrolTime;
        public static readonly ReactiveProperty<bool> Claiming = new(false);

        private const string PatrolRewardPushIdentifierKey = "PATROL_REWARD_PUSH_IDENTIFIER";
        private static Address? _currentAvatarAddress = null;

        public static bool NeedToInitialize(Address avatarAddress) =>
            !_currentAvatarAddress.HasValue || _currentAvatarAddress != avatarAddress;

        public static bool CanClaim =>
            _currentAvatarAddress.HasValue && !Claiming.Value && PatrolTime.Value >= Interval;

        static PatrolReward()
        {
            PatrolTime = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
                .CombineLatest(LastRewardTime, (_, lastReward) =>
                {
                    var timeSpan = DateTime.Now - lastReward;
                    return timeSpan > Interval ? Interval : timeSpan;
                })
                .ToReactiveProperty();
            LastRewardTime.ObserveOnMainThread()
                .Select(lastRewardTime => lastRewardTime + Interval - DateTime.Now)
                .Subscribe(SetPushNotification);
        }

        // Called at CurrentAvatarState isNewlySelected
        public static void InitializeInformation(Address avatarAddress, int level,
            long lastClaimedBlockIndex, long currentBlockIndex)
        {
            LoadPolicyInfo(level, currentBlockIndex);

            SetAvatarModel(avatarAddress, lastClaimedBlockIndex);

            // for changed avatar
            Claiming.Value = false;
        }

        public static void LoadAvatarInfo(Address avatarAddress, long lastClaimedBlockIndex)
        {
            SetAvatarModel(avatarAddress, lastClaimedBlockIndex);
        }

        public static void LoadPolicyInfo(int level, long blockIndex)
        {
            var patrolRewardSheet = Game.Game.instance.TableSheets.PatrolRewardSheet;
            var row = patrolRewardSheet.FindByLevel(level, blockIndex);
            var rewards = new List<PatrolRewardModel>();
            foreach (var rewardModel in row.Rewards)
            {
                rewards.Add(new PatrolRewardModel
                {
                    Currency = rewardModel.Ticker,
                    ItemId = rewardModel.ItemId,
                    PerInterval = rewardModel.Count,
                });
            }
            var policy = new PolicyModel
            {
                MinimumLevel = row.MinimumLevel,
                MaxLevel = row.MaxLevel,
                RequiredBlockInterval = row.Interval,
                Rewards = rewards,
            };
            SetPolicyModel(policy);
        }

        public static async void ClaimReward(System.Action onSuccess)
        {
            Claiming.Value = true;
            while (true)
            {
                onSuccess?.Invoke();
                break;
            }

            Claiming.Value = false;
        }

        private static void SetAvatarModel(Address avatarAddress, long lastClaimedBlockIndex)
        {
            // var lastClaimedAt = avatar.LastClaimedAt ?? avatar.CreatedAt;
            // LastRewardTime.Value = DateTime.Parse(lastClaimedAt);
            _currentAvatarAddress = avatarAddress;
        }

        private static void SetPolicyModel(PolicyModel policy)
        {
            NextLevel = policy.MaxLevel ?? int.MaxValue;
            Interval = policy.MinimumRequiredInterval;
            RewardModels.Value = policy.Rewards;
        }

        private static void SetPushNotification(TimeSpan completeTime)
        {
            var prevPushIdentifier = PlayerPrefs.GetString(PatrolRewardPushIdentifierKey, string.Empty);
            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(PatrolRewardPushIdentifierKey);
            }

            var pushIdentifier = PushNotifier.Push(
                L10nManager.Localize("PUSH_PATROL_REWARD_COMPLETE_CONTENT"),
                completeTime,
                PushNotifier.PushType.Reward);
            PlayerPrefs.SetString(PatrolRewardPushIdentifierKey, pushIdentifier);
        }
    }
}

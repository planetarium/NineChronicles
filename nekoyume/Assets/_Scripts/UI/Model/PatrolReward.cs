using System;
using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.ApiClient
{
    using UniRx;

    public static class PatrolReward
    {
        public static readonly ReactiveProperty<long> LastRewardClaimedBlockIndex = new();
        public static int NextLevel { get; private set; }
        public static long Interval { get; private set; }
        public static readonly ReactiveProperty<List<PatrolRewardModel>> RewardModels = new();

        public static readonly ReactiveProperty<long> PatrolTime = new();
        public static readonly ReactiveProperty<bool> Claiming = new(false);

        private const string PatrolRewardPushIdentifierKey = "PATROL_REWARD_PUSH_IDENTIFIER";
        private static Address? _currentAvatarAddress;

        public static bool NeedToInitialize(Address avatarAddress) =>
            !_currentAvatarAddress.HasValue || _currentAvatarAddress != avatarAddress;

        public static bool CanClaim =>
            _currentAvatarAddress.HasValue && !Claiming.Value && (PatrolTime.Value >= Interval || PatrolTime.Value == 0);

        static PatrolReward()
        {
            var agent = Game.Game.instance.Agent;
            agent.BlockIndexSubject
                .Subscribe(OnUpdateBlockIndex)
                .AddTo(Game.Game.instance);

            LastRewardClaimedBlockIndex.ObserveOnMainThread()
                .Select(lastRewardTime => lastRewardTime + Interval - Game.Game.instance.Agent.BlockIndex)
                .Subscribe(SetPushNotification);

            ReactiveAvatarState.ObservablePatrolRewardClaimedBlockIndex
                .Subscribe(blockIndex => LastRewardClaimedBlockIndex.Value = blockIndex)
                .AddTo(Game.Game.instance);
        }

        private static void OnUpdateBlockIndex(long blockIndex)
        {
            var lastRewardTime = LastRewardClaimedBlockIndex.Value;
            var patrolTime = blockIndex - lastRewardTime;
            PatrolTime.Value = Math.Min(Interval, patrolTime);
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
            try
            {
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
            catch (InvalidOperationException)
            {
                Debug.LogError("No activated policy matches the criteria.");
            }
        }

        public static void ClaimReward(System.Action onSuccess)
        {
            Claiming.Value = true;
            ActionManager.Instance.ClaimPatrolReward()
                .Subscribe(_ =>
                {
                    onSuccess?.Invoke();
                });
        }

        private static void SetAvatarModel(Address avatarAddress, long lastClaimedBlockIndex)
        {
            LastRewardClaimedBlockIndex.Value = lastClaimedBlockIndex;
            _currentAvatarAddress = avatarAddress;
        }

        private static void SetPolicyModel(PolicyModel policy)
        {
            NextLevel = policy.MaxLevel ?? int.MaxValue;
            Interval = policy.RequiredBlockInterval;
            RewardModels.Value = policy.Rewards;
        }

        private static void SetPushNotification(long completeTime)
        {
            var prevPushIdentifier = PlayerPrefs.GetString(PatrolRewardPushIdentifierKey, string.Empty);
            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(PatrolRewardPushIdentifierKey);
            }

            var pushIdentifier = PushNotifier.Push(
                L10nManager.Localize("PUSH_PATROL_REWARD_COMPLETE_CONTENT"),
                completeTime.BlockToTimeSpan(),
                PushNotifier.PushType.Reward);
            PlayerPrefs.SetString(PatrolRewardPushIdentifierKey, pushIdentifier);
        }
    }
}

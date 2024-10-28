using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.GraphQL;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
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
        public readonly ReactiveProperty<bool> Claiming = new(false);

        private const string PatrolRewardPushIdentifierKey = "PATROL_REWARD_PUSH_IDENTIFIER";
        public bool Initialized;

        public bool CanClaim => Initialized && !Claiming.Value && PatrolTime.Value >= Interval;

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

            // for changed avatar
            // Claiming.Value = false;
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

        public async void ClaimReward(System.Action onSuccess)
        {
            Claiming.Value = true;

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var txId = await PatrolRewardQuery.ClaimReward(avatarAddress.ToHex(), agentAddress.ToHex());
            while (true)
            {
                var txResultResponse = await TxResultQuery.QueryTxResultAsync(txId);
                if (txResultResponse is null)
                {
                    NcDebug.LogError(
                        $"Failed getting response : {nameof(TxResultQuery.TxResultResponse)}");
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_PATROL_REWARD_CLAIMED_FAILE"),
                        NotificationCell.NotificationType.Alert);
                    break;
                }

                var currentAvatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
                var txStatus = txResultResponse.transaction.transactionResult.txStatus;
                if (txStatus == TxResultQuery.TxStatus.SUCCESS)
                {
                    if (avatarAddress != currentAvatarAddress)
                    {
                        return;
                    }

                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_PATROL_REWARD_CLAIMED"),
                        NotificationCell.NotificationType.Notification);

                    onSuccess?.Invoke();
                    break;
                }

                if (txStatus == TxResultQuery.TxStatus.FAILURE)
                {
                    if (avatarAddress != currentAvatarAddress)
                    {
                        return;
                    }

                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_PATROL_REWARD_CLAIMED_FAILE"),
                        NotificationCell.NotificationType.Alert);
                    break;
                }

                await Task.Delay(3000);
            }

            Claiming.Value = false;
            await LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
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

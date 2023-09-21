using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.GraphQL;
using Nekoyume.L10n;
using Nekoyume.UI.Model.Patrol;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class PatrolRewardPopup : PopupWidget
    {
        [Serializable]
        private class RewardView
        {
            public GameObject container;
            public TextMeshProUGUI text;
        }

        [Serializable]
        private class RewardData
        {
            public PatrolRewardType rewardType;
            public RewardView cumulativeReward;
            public RewardView rewardPerTime;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private RewardData[] rewardViews;
        [SerializeField] private TextMeshProUGUI patrolTimeText;
        [SerializeField] private Image patrolTimeGauge;
        [SerializeField] private TextMeshProUGUI gaugeUnitText1;
        [SerializeField] private TextMeshProUGUI gaugeUnitText2;
        [SerializeField] private ConditionalButton receiveButton;

        public readonly PatrolReward PatrolReward = new();
        private readonly Dictionary<PatrolRewardType, int> _rewards = new();
        private readonly List<IDisposable> _disposables = new();

        private bool _initialized;
        private bool _claiming;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            CloseWidget = () => Close(true);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (_claiming)
            {
                return;
            }

            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            if (!PatrolReward.Initialized)
            {
                await PatrolReward.Initialize();
            }

            if (!_initialized)
            {
                Init();
            }

            var level = Game.Game.instance.States.CurrentAvatarState.level;
            if (PatrolReward.NextLevel <= level)
            {
                await PatrolReward.LoadPolicyInfo(level);

                gaugeUnitText1.text = GetTimeString(PatrolReward.Interval / 2);
                gaugeUnitText2.text = GetTimeString(PatrolReward.Interval);
            }

            OnChangeTime(PatrolReward.PatrolTime.Value);
            base.Show(ignoreShowAnimation);
        }

        private void Init()
        {
            _disposables.DisposeAllAndClear();

            PatrolReward.RewardModels
                .Subscribe(OnChangeRewardModels)
                .AddTo(_disposables);

            PatrolReward.PatrolTime
                .Where(_ => gameObject.activeSelf)
                .Subscribe(OnChangeTime)
                .AddTo(_disposables);

            receiveButton.OnSubmitSubject
                .Where(_ => PatrolReward.Initialized)
                .Subscribe(_ => ClaimRewardAsync())
                .AddTo(_disposables);

            _initialized = true;
        }

        // rewardModels
        private void OnChangeRewardModels(List<PatrolRewardModel> rewardModels)
        {
            SetRewards(rewardModels);
        }

        // time
        private void OnChangeTime(TimeSpan patrolTime)
        {
            patrolTimeText.text =
                L10nManager.Localize("UI_PATROL_TIME_FORMAT", GetTimeString(patrolTime));
            patrolTimeGauge.fillAmount = (float)(patrolTime / PatrolReward.Interval);

            var patrolTimeWithOutSeconds =
                new TimeSpan(patrolTime.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            var remainTime = PatrolReward.Interval - patrolTimeWithOutSeconds;
            var canReceive = remainTime <= TimeSpan.Zero;

            receiveButton.Interactable = canReceive && !_claiming;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", GetTimeString(remainTime));
        }

        private static string GetTimeString(TimeSpan time)
        {
            var hourExist = time.TotalHours >= 1;
            var minuteExist = time.Minutes >= 1;
            var hourText = hourExist ? $"{(int)time.TotalHours}h " : string.Empty;
            var minuteText = minuteExist || !hourExist ? $"{time.Minutes}m" : string.Empty;
            return $"{hourText}{minuteText}";
        }

        private void OnChangeRewards(Dictionary<PatrolRewardType, int> rewards)
        {
            foreach (var rewardView in rewardViews)
            {
                rewardView.cumulativeReward.container.SetActive(false);
            }

            foreach (var (rewardType, value) in rewards)
            {
                var view = rewardViews.FirstOrDefault(view => view.rewardType == rewardType);
                if (view == null)
                {
                    continue;
                }

                view.cumulativeReward.container.SetActive(value > 0);
                view.cumulativeReward.text.text = $"{value}";
            }
        }

        private void SetRewards(List<PatrolRewardModel> rewardModels)
        {
            _rewards.Clear();

            foreach (var reward in rewardModels)
            {
                var rewardType = GetRewardType(reward);
                _rewards[rewardType] = reward.PerInterval;
            }

            OnChangeRewards(_rewards);
        }

        private static PatrolRewardType GetRewardType(PatrolRewardModel reward)
        {
            if (reward.ItemId != null)
            {
                return reward.ItemId switch
                {
                    500000 => PatrolRewardType.ApStone,
                    600201 => PatrolRewardType.GoldPowder,
                    800201 => PatrolRewardType.SilverPowder,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return reward.Currency switch
            {
                "CRYSTAL" => PatrolRewardType.Crystal,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async void ClaimRewardAsync()
        {
            _claiming = true;
            receiveButton.Interactable = !_claiming;

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var txId = await PatrolReward.ClaimReward(avatarAddress.ToHex(), agentAddress.ToHex());

            Close();

            while (true)
            {
                var txResultResponse = await TxResultQuery.QueryTxResultAsync(txId);
                if (txResultResponse is null)
                {
                    Debug.LogError($"Failed getting response : {nameof(TxResultQuery.TxResultResponse)}");
                    break;
                }

                var txStatus = txResultResponse.transaction.transactionResult.txStatus;
                if (txStatus == TxResultQuery.TxStatus.SUCCESS)
                {
                    break;
                }

                await Task.Delay(3000);
            }
            _claiming = false;

            Find<Menu>().PatrolRewardMenu.ClaimRewardAnimation();
            await PatrolReward.LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
        }
    }
}

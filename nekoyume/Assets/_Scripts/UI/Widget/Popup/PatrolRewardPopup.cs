using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Lobby;
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
        private class BonusRewardView : RewardView
        {
            public GameObject[] unlockObjects;
            public GameObject[] lockObjects;
        }

        [Serializable]
        private class RewardData
        {
            public CostType costType;
            public RewardView cumulativeReward;
            public RewardView rewardPerTime;
        }

        [Serializable]
        private class BonusRewardData
        {
            public CostType costType;
            public BonusRewardView cumulativeReward;
            public BonusRewardView rewardPerTime;
        }

        [Serializable]
        private struct PatrolTimeStruct
        {
            public int hour;
            public int minute;
        }

        [SerializeField] private Button closeButton;

        [SerializeField] private RewardData[] rewardViews;
        [SerializeField] private TextMeshProUGUI patrolTimeText;
        [SerializeField] private Image patrolTimeGauge;
        [SerializeField] private ConditionalButton receiveButton;

        [SerializeField] private BonusRewardData[] bonusRewardViews;
        [SerializeField] private Image patrolTimeBonusGauge;
        [SerializeField] private GameObject bonusGaugeLockObject;
        [SerializeField] private Button bonusUnlockButton;
        [SerializeField] private TextMeshProUGUI bonusCostText;

        private const bool BonusReward = false;
        private static readonly TimeSpan MaxTime = new(24, 0, 0);
        private static readonly TimeSpan MinTime = new(0, 10, 0);
        private readonly List<IDisposable> _disposables = new();

        private Dictionary<CostType, int> _rewards;
        private Dictionary<CostType, int> _rewardsPerTime;
        private DateTime _lastRewardTime;
        private TimeSpan PatrolTime
        {
            get
            {
                var patrolTime = DateTime.Now - _lastRewardTime;
                return patrolTime > MaxTime ? MaxTime : patrolTime;
            }
        }

        // 임시 데이터
        [SerializeField] private int menuLevelForTest;
        [SerializeField] private int levelForTest;
        [SerializeField] private PatrolTimeStruct patrolTimeForTest;
        private static readonly Dictionary<int, Dictionary<CostType, int>> RewardTable = new()
        {
            [1] = new Dictionary<CostType, int>
            {
                [CostType.Crystal] = 50,
                [CostType.ActionPoint] = 1,
                [CostType.SilverDust] = 50,
                [CostType.GoldDust] = 1,
            },
            [150] = new Dictionary<CostType, int>
            {
                [CostType.Crystal] = 125,
                [CostType.ActionPoint] = 1,
                [CostType.SilverDust] = 60,
                [CostType.GoldDust] = 1,
            },
            [250] = new Dictionary<CostType, int>
            {
                [CostType.Crystal] = 250,
                [CostType.ActionPoint] = 1,
                [CostType.SilverDust] = 70,
                [CostType.GoldDust] = 2,
            },
        };

        private static readonly Dictionary<CostType, TimeSpan> UnitTable = new()
        {
            [CostType.GoldDust] = new TimeSpan(8, 0, 0),
            [CostType.SilverDust] = new TimeSpan(6, 0, 0),
            [CostType.ActionPoint] = new TimeSpan(24, 0, 0),
            [CostType.Crystal] = new TimeSpan(0, 5, 0)
        };

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
            base.Show(ignoreShowAnimation);

            _disposables.DisposeAllAndClear();
            _lastRewardTime = DateTime.Today; // Todo : Fill this

            FindObjectOfType<PatrolRewardMenu>().SetLevel(menuLevelForTest, false);

            var patrolTime = new TimeSpan(patrolTimeForTest.hour, patrolTimeForTest.minute, 0);
            Observable
                .Timer(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1))
                .Subscribe(_ => patrolTime = patrolTime.Add(TimeSpan.FromMinutes(1)))
                // .Subscribe(_ => _patrolTime = DateTime.Now - lastRewardTime)
                .AddTo(_disposables);

            // var level = States.Instance.CurrentAvatarState.level;
            var level = levelForTest;
            var rewardLevel = 0;
            foreach (var key in RewardTable.Keys)
            {
                if (level >= key)
                {
                    rewardLevel = key;
                }
                else
                {
                    break;
                }
            }
            _rewardsPerTime = RewardTable[rewardLevel];

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ReceiveReward(_rewards))
                .AddTo(_disposables);
        }

        // rewardPerTime
        private void OnChangeRewardsPerTime(Dictionary<CostType, int> rewardsPerTime)
        {
            foreach (var bonusRewardView in rewardViews)
            {
                bonusRewardView.rewardPerTime.container.SetActive(false);
            }

            foreach (var (costType, value) in rewardsPerTime)
            {
                var view = rewardViews.FirstOrDefault(view => view.costType == costType);
                if (view == null)
                {
                    continue;
                }

                var unitTime = UnitTable[costType];
                var unitText = unitTime.TotalHours > 1
                    ? $"{unitTime.TotalHours}h"
                    : $"{unitTime.TotalMinutes}m";
                view.rewardPerTime.container.SetActive(true);
                view.rewardPerTime.text.text = $"{value}/{unitText}";
            }

            SetRewards(rewardsPerTime, PatrolTime);
        }

        // time
        private void OnChangeTime()
        {
            var patrolTime = PatrolTime;

            patrolTimeText.text = L10nManager.Localize(
                "UI_PATROL_TIME_FORMAT",
                $"{patrolTime.TotalHours}h {patrolTime.Minutes}m");
            patrolTimeGauge.fillAmount = (float)(patrolTime / MaxTime);

            var remainTime = MinTime - patrolTime;
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", remainTime.ToString(@"m'm'"));

            SetRewards(_rewardsPerTime, patrolTime);
        }

        // rewardPerTime, patrolTime
        private void SetRewards(Dictionary<CostType, int> rewardsPerTime, TimeSpan patrolTime)
        {
            _rewards.Clear();

            var clampedPatrolTime = patrolTime > MaxTime ? MaxTime : patrolTime;
            foreach (var (costType, value) in rewardsPerTime)
            {
                _rewards[costType] = value * (int)(clampedPatrolTime / UnitTable[costType]);
            }
        }

        private void OnChangeRewards(Dictionary<CostType, int> rewards)
        {
            foreach (var bonusRewardView in rewardViews)
            {
                bonusRewardView.cumulativeReward.container.SetActive(false);
            }

            foreach (var (costType, value) in rewards)
            {
                var view = rewardViews.FirstOrDefault(view => view.costType == costType);
                if (view == null)
                {
                    continue;
                }

                view.cumulativeReward.container.SetActive(value > 0);
                view.cumulativeReward.text.text = $"{value}";
            }
        }

        private void ReceiveReward(Dictionary<CostType, int> rewards)
        {
            foreach (var reward in rewards)
            {
                Debug.Log($"Reward : {reward.Key} x{reward.Value}");
            }

            Close();
            // PatrolRewardMenu.DailyRewardAnimation
        }

        private void SetBonusData(Dictionary<CostType, int> bonusRewardPerTime, TimeSpan patrolTime, bool bonusLocked)
        {
            foreach (var bonusRewardView in bonusRewardViews)
            {
                bonusRewardView.rewardPerTime.container.SetActive(false);
                bonusRewardView.cumulativeReward.container.SetActive(false);
            }

            if (!BonusReward)
            {
                patrolTimeBonusGauge.gameObject.SetActive(false);
                bonusUnlockButton.gameObject.SetActive(false);
                return;
            }

            // rewardPerTime
            foreach (var (costType, value) in bonusRewardPerTime)
            {
                var view = bonusRewardViews.FirstOrDefault(view => view.costType == costType);
                if (view == null)
                {
                    continue;
                }

                var unit = UnitTable[costType];

                var unitText = unit.TotalHours > 1 ? $"{unit.TotalHours}h" : $"{unit.TotalMinutes}m";
                view.rewardPerTime.container.SetActive(true);
                view.rewardPerTime.text.text = $"{value}/{unitText}";
            }

            // rewardPerTime, patrol time
            foreach (var (costType, value) in bonusRewardPerTime)
            {
                var view = bonusRewardViews.FirstOrDefault(view => view.costType == costType);
                if (view == null)
                {
                    continue;
                }

                var unit = UnitTable[costType];

                var patrolTime2 = patrolTime > MaxTime ? MaxTime : patrolTime;
                var point = (int)(patrolTime2 / unit);
                view.cumulativeReward.container.SetActive(true);
                view.cumulativeReward.text.text = $"{value * point}";
            }

            // patrol time
            patrolTimeBonusGauge.fillAmount = (float)(patrolTime / MaxTime);

            // bonus locked
            foreach (var bonusRewardView in bonusRewardViews)
            {
                Array.ForEach(bonusRewardView.rewardPerTime.lockObjects,
                    o => o.SetActive(bonusLocked));
                Array.ForEach(bonusRewardView.rewardPerTime.unlockObjects,
                    o => o.SetActive(!bonusLocked));
                Array.ForEach(bonusRewardView.cumulativeReward.lockObjects,
                    o => o.SetActive(bonusLocked));
                Array.ForEach(bonusRewardView.cumulativeReward.unlockObjects,
                    o => o.SetActive(!bonusLocked));
            }

            bonusGaugeLockObject.SetActive(bonusLocked);
            bonusUnlockButton.gameObject.SetActive(bonusLocked);
            if (bonusLocked)
            {
                bonusCostText.text = ""; // Todo : Fill this
            }
        }
    }
}

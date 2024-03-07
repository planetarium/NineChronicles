using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;
    public class StakingMenu : MainMenu
    {
        [SerializeField]
        private GameObject notificationObj;

        [SerializeField]
        private GameObject notStakingObj;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI stakedNcgText;

        [SerializeField]
        private TimeBlock claimableTimeBlock;

        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(OnEveryUpdateBlockIndex)
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            _disposables.DisposeAllAndClear();
            StakingSubject.Level.Subscribe(level => levelText.text = $"Lv. {level}")
                .AddTo(_disposables);
            StakingSubject.StakedNCG.Subscribe(fav => stakedNcgText.text = fav.GetQuantityString())
                .AddTo(_disposables);
            levelText.text = $"Lv. {States.Instance.StakingLevel}";
            stakedNcgText.text = States.Instance.StakedBalanceState.Gold.GetQuantityString();
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void OnEveryUpdateBlockIndex(long tip)
        {
            var nullableStakeState = States.Instance.StakeStateV2;
            var hasStakeState = nullableStakeState.HasValue;
            bool enableNotification;
            var enableNotStaking = false;
            if (hasStakeState)
            {
                var remaining = nullableStakeState.Value.ClaimableBlockIndex - tip;
                enableNotification = remaining <= 0;
                claimableTimeBlock.SetTimeBlock($"{remaining:#,0}",remaining.BlockRangeToTimeSpanString());
            }
            else
            {
                var minimumNcg = States.Instance.StakeRegularRewardSheet
                        .First(pair => pair.Value.Level == 1)
                        .Value.RequiredGold;
                enableNotification = enableNotStaking =
                    States.Instance.GoldBalanceState.Gold.MajorUnit >= minimumNcg;
            }

            notificationObj.SetActive(enableNotification);
            notStakingObj.SetActive(enableNotStaking);
        }
    }
}

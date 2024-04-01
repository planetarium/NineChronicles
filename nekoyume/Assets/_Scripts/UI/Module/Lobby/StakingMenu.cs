using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
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

        [SerializeField]
        private GameObject levelTextParent;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            _disposables.DisposeAllAndClear();
            StakingSubject.Level.Subscribe(OnUpdateStakingLevel)
                .AddTo(_disposables);
            StakingSubject.StakedNCG.Subscribe(OnUpdateStakedBalance)
                .AddTo(_disposables);
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(OnEveryUpdateBlockIndex)
                .AddTo(_disposables);
            OnUpdateStakingLevel(States.Instance.StakingLevel);
            OnUpdateStakedBalance(States.Instance.StakedBalanceState.Gold);
            OnEveryUpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
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
                var remaining = Math.Max(nullableStakeState.Value.ClaimableBlockIndex - tip, 0);
                enableNotification = remaining <= 0;
                claimableTimeBlock.gameObject.SetActive(true);
                claimableTimeBlock.SetTimeBlock($"{remaining:#,0}",remaining.BlockRangeToTimeSpanString());
            }
            else
            {
                var minimumNcg = States.Instance.StakeRegularRewardSheet
                        .First(pair => pair.Value.Level == 1)
                        .Value.RequiredGold;
                enableNotification = enableNotStaking =
                    States.Instance.GoldBalanceState.Gold.MajorUnit >= minimumNcg;
                claimableTimeBlock.gameObject.SetActive(false);
            }

            notificationObj.SetActive(enableNotification);
            notStakingObj.SetActive(enableNotStaking);
        }

        private void OnUpdateStakingLevel(int level)
        {
            levelText.text = $"Lv. {level}";
        }

        private void OnUpdateStakedBalance(FungibleAssetValue fav)
        {
            stakedNcgText.text = fav.GetQuantityString();
            levelTextParent.gameObject.SetActive(fav.MajorUnit > 0);
        }
    }
}

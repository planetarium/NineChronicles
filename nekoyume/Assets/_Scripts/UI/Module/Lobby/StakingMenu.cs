using System.Linq;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    public class StakingMenu : MainMenu
    {
        [SerializeField]
        private GameObject notificationObj;

        [SerializeField]
        private GameObject notStakingObj;

        protected override void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(OnEveryUpdateBlockIndex)
                .AddTo(gameObject);
        }

        private void OnEveryUpdateBlockIndex(long tip)
        {
            var nullableStakeState = States.Instance.StakeStateV2;
            var hasStakeState = nullableStakeState.HasValue;
            bool enableNotification;
            var enableNotStaking = false;
            if (hasStakeState)
            {
                enableNotification = nullableStakeState.Value.ClaimableBlockIndex <= tip;
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

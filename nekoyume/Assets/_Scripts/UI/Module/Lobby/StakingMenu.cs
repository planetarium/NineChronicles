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

        protected override void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(OnEveryUpdateBlockIndex)
                .AddTo(gameObject);
        }

        private void OnEveryUpdateBlockIndex(long tip)
        {
            var nullableStakeState = States.Instance.StakeStateV2;
            var hasStakeState = nullableStakeState.HasValue;
            if (hasStakeState)
            {
                if (nullableStakeState.Value.ClaimableBlockIndex <= tip)
                {
                    notificationObj.SetActive(true);
                    return;
                }
            }
            else
            {
                var minimumNcg = States.Instance.StakeRegularRewardSheet
                        .First(pair => pair.Value.Level == 1)
                        .Value.RequiredGold;
                notificationObj.SetActive(States.Instance.GoldBalanceState.Gold.MajorUnit >=
                                          minimumNcg);
                return;
            }

            notificationObj.SetActive(false);
        }
    }
}

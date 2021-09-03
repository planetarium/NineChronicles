using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : Widget
    {
        [SerializeField]
        private Button ArenaEventButton;

        [SerializeField]
        private Button playToEarnGoldEventButton;

        [SerializeField]
        private Button playToEarnInviteEventButton;

        private const string ArenaEventPageURLFormat = "https://ninechronicles.medium.com/announcing-nine-chronicles-arena-season-0-896k-ncg-prize-pool-season-exclusive-nfts-%EF%B8%8F-ce0b12bc7e08";

        private const string GoldEventPageURLFormat = "https://onboarding.nine-chronicles.com/earn?nc_address={0}";

        private const string InvitePageURLFormat = "https://onboarding.nine-chronicles.com/invite?nc_address={0}";

        private void Awake()
        {
            ArenaEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToArenaEventPage())
                .AddTo(gameObject);

            playToEarnGoldEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToGoldEventPage())
                .AddTo(gameObject);

            playToEarnInviteEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToInviteEventPage())
                .AddTo(gameObject);
        }

        private void GoToArenaEventPage()
        {
            var address = States.Instance.AgentState.address;
            var url = string.Format(ArenaEventPageURLFormat, address);
            Application.OpenURL(url);
        }

        private void GoToGoldEventPage()
        {
            var address = States.Instance.AgentState.address;
            var url = string.Format(GoldEventPageURLFormat, address);
            Application.OpenURL(url);
        }

        private void GoToInviteEventPage()
        {
            var address = States.Instance.AgentState.address;
            var url = string.Format(InvitePageURLFormat, address);
            Application.OpenURL(url);
        }
    }
}

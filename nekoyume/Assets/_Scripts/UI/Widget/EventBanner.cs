using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : Widget
    {
        [SerializeField]
        private Button itemLevelNoticeButton;
        
        // [SerializeField]
        // private Button ArenaEventButton;

        // [SerializeField]
        // private Button bigCatYearEventButton;

        [SerializeField]
        private Button playToEarnGoldEventButton;

        [SerializeField]
        private Button playToEarnInviteEventButton;

        private const string ItemLevelNoticePageURLFormat = "https://www.notion.so/planetarium/1bc6de399b3b4ace95fca3a3020b4d79";
        
        private const string ArenaEventPageURLFormat = "https://ninechronicles.medium.com/nine-chronicles-arena-season-2-224k-ncg-reward-pool-begins-march-4th-88c947d507d6";

        private const string bigCatYearEventPageURLFormat = "https://onboarding.nine-chronicles.com/";

        private const string GoldEventPageURLFormat = "https://onboarding.nine-chronicles.com/earn?nc_address={0}";

        private const string InvitePageURLFormat = "https://onboarding.nine-chronicles.com/invite?nc_address={0}";

        private void Awake()
        {
            itemLevelNoticeButton.OnClickAsObservable()
                .Subscribe(_ => GoToItemLevelNoticePage())
                .AddTo(gameObject);
            
            // ArenaEventButton.onClick.AsObservable()
            //     .Subscribe(_ => GoToArenaEventPage())
            //     .AddTo(gameObject);

            // bigCatYearEventButton.onClick.AsObservable()
            //     .Subscribe(_ => GoTobigCatYearEventPage())
            //     .AddTo(gameObject);

            playToEarnGoldEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToGoldEventPage())
                .AddTo(gameObject);

            playToEarnInviteEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToInviteEventPage())
                .AddTo(gameObject);
        }

        private void GoToItemLevelNoticePage()
        {
            Application.OpenURL(ItemLevelNoticePageURLFormat);
        }
        
        private void GoToArenaEventPage()
        {
            var address = States.Instance.AgentState.address;
            var url = string.Format(ArenaEventPageURLFormat, address);
            Application.OpenURL(url);
        }

        private void GoTobigCatYearEventPage()
        {
            Application.OpenURL(bigCatYearEventPageURLFormat);
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

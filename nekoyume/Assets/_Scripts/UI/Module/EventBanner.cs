using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : MonoBehaviour
    {
        [SerializeField]
        private Button playToEarnGoldEventButton;

        [SerializeField]
        private Button playToEarnInviteEventButton;

        private const string GoldEventPageURLFormat = "https://onboarding.nine-chronicles.com/earn?nc_address={0}";

        private const string InvitePageURLFormat = "https://onboarding.nine-chronicles.com/invite?nc_address={0}";

        private void Awake()
        {
            playToEarnGoldEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToGoldEventPage())
                .AddTo(gameObject);

            playToEarnInviteEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToInviteEventPage())
                .AddTo(gameObject);
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

using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : MonoBehaviour
    {
        [SerializeField]
        private Button playToEarnEventEarnGoldButton;

        [SerializeField]
        private Button playToEarnEventInviteFriendsButton;

        private const string EventPageURLFormat = "https://onboarding.nine-chronicles.com/nc-address?prefill={0}";

        private void Awake()
        {
            playToEarnEventEarnGoldButton.onClick.AsObservable()
                .Subscribe(_ => GoToEventPage())
                .AddTo(gameObject);

            playToEarnEventInviteFriendsButton.onClick.AsObservable()
                .Subscribe(_ => GoToEventPage())
                .AddTo(gameObject);
        }

        private void GoToEventPage()
        {
            var address = States.Instance.AgentState.address;
            var url = string.Format(EventPageURLFormat, address);
            Application.OpenURL(url);
        }
    }
}

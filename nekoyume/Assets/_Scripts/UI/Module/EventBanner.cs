using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBanner : MonoBehaviour
    {
        [SerializeField]
        private Button playToEarnEventButton;

        private void Awake()
        {
            playToEarnEventButton.onClick.AsObservable()
                .Subscribe(_ => GoToEventPage())
                .AddTo(gameObject);
        }

        private void GoToEventPage()
        {
            var address = Game.Game.instance.States.CurrentAvatarState.address;

            var confirm = Widget.Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                {
                    return;
                }

                Application.OpenURL("https://nine-chronicles.com/");
            };
            confirm.Show("UI_PROCEED_EVENTPAGE", address.ToString(), blurRadius: 2);
        }
    }
}

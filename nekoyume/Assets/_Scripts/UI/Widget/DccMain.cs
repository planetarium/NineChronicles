using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DccMain : Widget
    {
        [SerializeField]
        private Button collectionButton;

        [SerializeField]
        private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            collectionButton.onClick.AddListener(() =>
            {
                Find<DccCollection>().Show();
            });
            backButton.onClick.AddListener(() =>
            {
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }
    }
}

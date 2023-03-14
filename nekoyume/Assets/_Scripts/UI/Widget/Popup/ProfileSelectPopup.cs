using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ProfileSelectPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Button dccShortCutButton;

        [SerializeField]
        private GameObject[] onDccLocked;

        [SerializeField]
        private GameObject[] onDccUnlocked;

        [SerializeField]
        private FramedCharacterView avatarCharacterView;

        [SerializeField]
        private FramedCharacterView dccCharacterView;

        [SerializeField]
        private ConditionalButton avatarButton;

        [SerializeField]
        private ConditionalButton dccButton;

        private const string DccShortCutUrl = "https://dcc.nine-chronicles.com/staking";

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
            dccShortCutButton.onClick.AddListener(() => Application.OpenURL(DccShortCutUrl));
        }

        public void Show()
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var isDccActive = Dcc.instance.Avatars
                .TryGetValue(avatarState.address.ToString(), out var dccId);

            avatarCharacterView.SetByAvatarState(avatarState);
            if (isDccActive)
            {
                dccCharacterView.SetByDccId(dccId);
            }

            // is DCC locked?
            // Y -> show avatar profile -> enable Avatar selected / DCC locked
            // N -> show DCC profile -> enable DCC selected / Avatar select
            foreach (var obj in onDccLocked)
            {
                obj.SetActive(!isDccActive);
            }

            foreach (var obj in onDccUnlocked)
            {
                obj.SetActive(isDccActive);
            }

            // button - state : Normal - Select, Conditional - Locked, Disabled - Selected
            avatarButton.Interactable = isDccActive;
            avatarButton.SetState(!isDccActive
                ? ConditionalButton.State.Disabled
                : ConditionalButton.State.Normal);
            dccButton.Interactable = !isDccActive;
            dccButton.SetState(isDccActive
                ? ConditionalButton.State.Disabled
                : ConditionalButton.State.Conditional);

            base.Show();
        }
    }
}

using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;
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
        private ConditionalButton avatarButton;

        [SerializeField]
        private ConditionalButton dccButton;

        [SerializeField]
        private Image avatarImage;

        [SerializeField]
        private Image pfpImage;

        private const string DccShortCutUrl = "https://dcc.nine-chronicles.com/staking";

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
            dccShortCutButton.onClick.AddListener(() => Application.OpenURL(DccShortCutUrl));
        }

        public void Show()
        {
            var id = Util.GetPortraitId(BattleType.Adventure);
            // var image = SpriteHelper.GetItemIcon(id);
            // var image = SpriteHelper.GetCharacterIcon(player.Model.RowData.Id);
            avatarImage.sprite = SpriteHelper.GetItemIcon(id);

            var dcc = Game.Game.instance.Dcc;
            var address = States.Instance.CurrentAvatarState.address.ToString();
            var isProfileExist = dcc.Avatars.TryGetValue(address, out var pfpId);
            if (isProfileExist)
            {
                pfpImage.sprite = SpriteHelper.GetPfpProfileIcon((int)pfpId);
            }

            // is DCC locked?
            // Y -> show avatar profile -> enable Avatar selected / DCC locked
            // N -> show DCC profile -> enable DCC selected / Avatar select
            foreach (var obj in onDccLocked)
            {
                obj.SetActive(!isProfileExist);
            }
            foreach (var obj in onDccUnlocked)
            {
                obj.SetActive(isProfileExist);
            }

            // button - state : Normal - Select, Conditional - Locked, Disabled - Selected
            avatarButton.Interactable = isProfileExist;
            avatarButton.SetState(!isProfileExist
                ? ConditionalButton.State.Disabled
                : ConditionalButton.State.Normal);
            dccButton.Interactable = !isProfileExist;
            dccButton.SetState(isProfileExist
                ? ConditionalButton.State.Disabled
                : ConditionalButton.State.Conditional);

            base.Show();
        }
    }
}

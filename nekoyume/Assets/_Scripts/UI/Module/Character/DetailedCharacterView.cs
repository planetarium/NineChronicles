using Nekoyume.Model.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class DetailedCharacterView : FramedCharacterView
    {
        [SerializeField]
        private TextMeshProUGUI levelText = null;

        [SerializeField]
        private Button button = null;

        private AvatarState _avatarStateToDisplay;

        protected void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        public override void SetByAvatarState(AvatarState avatarState)
        {
            base.SetByAvatarState(avatarState);
            levelText.text = $"Lv.{avatarState.level}";
            _avatarStateToDisplay = avatarState;
        }

        protected void OnClickButton()
        {
            if (_avatarStateToDisplay is null)
            {
                return;
            }

            var friendInfoPopup = Widget.Find<FriendInfoPopup>();
            friendInfoPopup.Show(_avatarStateToDisplay);
        }
    }
}

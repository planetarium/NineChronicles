using Nekoyume.Model.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class DetailedCharacterView : FramedCharacterView
    {
        [SerializeField]
        private TextMeshProUGUI levelText = null;

        public override void SetByAvatarState(AvatarState avatarState)
        {
            base.SetByAvatarState(avatarState);
            levelText.text = $"Lv.{avatarState.level}";
        }
    }
}

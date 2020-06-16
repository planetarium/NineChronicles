using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class FramedCharacterView : VanillaCharacterView
    {
        [SerializeField]
        private Image frameImage = null;

        [SerializeField]
        private bool isTitleFrame = false;

        public override void SetByAvatarState(AvatarState avatarState)
        {
            base.SetByAvatarState(avatarState);

            if (!isTitleFrame)
            {
                return;
            }

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            if (title is null)
            {
                SetFrame(null);
            }
            else
            {
                var image = SpriteHelper.GetTitleFrame(title.Id);
                SetFrame(image);
            }
        }

        public override void SetByPlayer(Player player)
        {
            base.SetByPlayer(player);

            if (!isTitleFrame)
            {
                return;
            }

            var title = player.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            if (title is null)
            {
                SetFrame(null);
            }
            else
            {
                var image = SpriteHelper.GetTitleFrame(title.Id);
                SetFrame(image);
            }
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            var alpha = isDim ? .3f : 1f;
            frameImage.color = GetColor(frameImage.color, alpha);
        }

        private void SetFrame(Sprite image)
        {
            if (image is null)
            {
                frameImage.enabled = false;
                return;
            }

            frameImage.overrideSprite = image;
            frameImage.SetNativeSize();
            frameImage.enabled = true;
        }
    }
}

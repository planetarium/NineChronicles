using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using UnityEngine;
using UnityEngine.UI;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.UI.Module
{
    public class VanillaCharacterView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetByAvatarState(AvatarState avatarState)
        {
            var id = avatarState.GetArmorIdForPortrait();
            SetByFullCostumeOrArmorId(id);
        }

        public virtual void SetByPlayer(Player player)
        {
            var id = Util.GetPortraitId(BattleType.Adventure);
            SetByFullCostumeOrArmorId(id);
        }

        public virtual void SetByCharacterId(int characterId)
        {
            var image = SpriteHelper.GetCharacterIcon(characterId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());
            }

            SetIcon(image);
        }

        public virtual void SetByFullCostumeOrArmorId(int armorOrFullCostumeId)
        {
            var image = SpriteHelper.GetItemIcon(armorOrFullCostumeId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(armorOrFullCostumeId.ToString());
            }

            SetIcon(image);
        }

        protected virtual void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            iconImage.color = GetColor(iconImage.color, alpha);
        }

        protected static Color GetColor(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        protected void SetIcon(Sprite image)
        {
            iconImage.sprite = image;
            iconImage.enabled = true;
        }
    }
}

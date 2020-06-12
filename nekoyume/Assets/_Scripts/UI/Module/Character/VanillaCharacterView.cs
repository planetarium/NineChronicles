using System.Linq;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
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

        public void SetByAvatarAddress(Address avatarAddress)
        {
            var iValue = Game.Game.instance.Agent.GetState(avatarAddress);
            var avatarState = new AvatarState((Bencodex.Types.Dictionary) iValue);
            SetByAvatarState(avatarState);
        }

        public void SetByAvatarState(AvatarState avatarState)
        {
            var fullCostume = avatarState.inventory.Costumes
                .FirstOrDefault(costume =>
                    costume.ItemSubType == ItemSubType.FullCostume &&
                    costume.equipped);
            if (!(fullCostume is null))
            {
                SetByArmorId(fullCostume.Id);
                return;
            }

            var armor = avatarState.inventory.Equipments
                .FirstOrDefault(equipment =>
                    equipment.ItemSubType == ItemSubType.Armor &&
                    equipment.equipped);
            if (!(armor is null))
            {
                SetByArmorId(armor.Id);
                return;
            }

            SetByCharacterId(avatarState.characterId);
        }

        public void SetByPlayer(Player player)
        {
            var fullCostume = player.Costumes
                .FirstOrDefault(costume => costume.ItemSubType == ItemSubType.FullCostume);
            if (!(fullCostume is null))
            {
                SetByArmorId(fullCostume.Id);
                return;
            }

            var armor = player.Equipments
                .FirstOrDefault(equipment => equipment.ItemSubType == ItemSubType.Armor);
            if (!(armor is null))
            {
                SetByArmorId(armor.Id);
                return;
            }

            SetByCharacterId(player.Model.RowData.Id);
        }

        public void SetByCharacterId(int characterId)
        {
            var image = SpriteHelper.GetCharacterIcon(characterId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());
            }

            SetIcon(image);
        }

        public void SetByArmorId(int armorId)
        {
            var image = SpriteHelper.GetItemIcon(armorId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(armorId.ToString());
            }

            SetIcon(image);
        }

        private void SetIcon(Sprite image)
        {
            iconImage.enabled = true;
            iconImage.overrideSprite = image;
            iconImage.SetNativeSize();
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
    }
}

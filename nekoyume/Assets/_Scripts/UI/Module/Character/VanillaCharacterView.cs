using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

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

        public void SetIconByCharacterId(int characterId)
        {
            var image = SpriteHelper.GetCharacterIcon(characterId);
            if (image is null)
            {
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());
            }

            SetIcon(image);
        }

        public void SetIconByArmorId(int armorId)
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

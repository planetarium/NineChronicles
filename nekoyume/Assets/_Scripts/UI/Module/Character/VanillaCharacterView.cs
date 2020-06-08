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
            var itemSprite = SpriteHelper.GetCharacterIcon(characterId);
            if (itemSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());
            }

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
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

using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class VanillaCharacterView : MonoBehaviour
    {
        public Image backgroundImage;
        public Image iconImage;
        
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }
        
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetData(int characterId)
        {
            var itemSprite = SpriteHelper.GetCharacterIcon(characterId);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(characterId.ToString());

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();
        }
        
        public virtual void Clear()
        {
            iconImage.enabled = false;
        }

        protected virtual void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            iconImage.color = GetColor(iconImage.color, alpha);
        }
        
        protected Color GetColor(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}

using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class VanillaSkillView : MonoBehaviour
    {
        public Image iconImage;
        public bool IsShown => gameObject.activeSelf;

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetData(int skillId)
        {
            var itemSprite = SpriteHelper.GetSkillIcon(skillId);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(skillId.ToString());

            SetData(itemSprite);
        }

        public virtual void SetData(Sprite sprite)
        {
            iconImage.overrideSprite = sprite;
            Show();
        }
    }
}

using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RecipeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage = null;
        [SerializeField] private Image enabledBgImage = null;
        [SerializeField] private Image disabledBgImage = null;
        [SerializeField] private Image elementImage = null;

        [SerializeField] private GameObject enabledObject = null;
        [SerializeField] private GameObject disabledObject = null;

        [SerializeField] private Image SelectedImage = null;

        public void SetData(ItemSheet.Row itemRow)
        {
            if (itemRow is null)
            {
                Lock();
            }

            var itemSprite = SpriteHelper.GetItemIcon(itemRow.Id);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(itemRow.Id.ToString());
            iconImage.overrideSprite = itemSprite;
            elementImage.sprite = itemRow.ElementalType.GetSprite();

            enabledObject.SetActive(true);
            disabledObject.SetActive(false);
        }

        public void Lock()
        {
            disabledObject.SetActive(true);
            enabledObject.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

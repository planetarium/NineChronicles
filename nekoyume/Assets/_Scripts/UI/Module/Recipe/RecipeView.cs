using Coffee.UIEffects;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RecipeView : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private Image enabledBgImage = null;

        [SerializeField]
        private UIHsvModifier levelBg = null;

        [SerializeField]
        private TextMeshProUGUI normalLevelText = null;

        [SerializeField]
        private TextMeshProUGUI lockedLevelText = null;

        [SerializeField]
        private Image elementImage = null;
        

        public void Show(RecipeViewData.Data viewData, ItemSheet.Row itemRow)
        {
            if (itemRow is null)
            {
                Hide();
                return;
            }

            var itemSprite = SpriteHelper.GetItemIcon(itemRow.Id);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(itemRow.Id.ToString());
            iconImage.overrideSprite = itemSprite;

            normalLevelText.fontSharedMaterial = viewData.LevelTextMaterial;
            levelBg.targetColor = viewData.LevelBgHsvTargetColor;
            levelBg.range = viewData.LevelBgHsvRange;
            levelBg.hue = viewData.LevelBgHsvHue;
            levelBg.saturation = viewData.LevelBgHsvSaturation;
            levelBg.value = viewData.LevelBgHsvValue;

            if (itemRow.ItemType == ItemType.Equipment)
            {
                elementImage.sprite = itemRow.ElementalType.GetSprite();
            }
            else if(itemRow.ItemType == ItemType.Consumable)
            {
                elementImage.enabled = false;
            }
            enabledBgImage.overrideSprite = viewData.BgSprite;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

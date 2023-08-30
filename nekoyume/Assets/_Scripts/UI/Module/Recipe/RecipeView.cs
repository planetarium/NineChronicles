using Coffee.UIEffects;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.State;
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

        [SerializeField]
        private TextMeshProUGUI countText = null;

        public void Show(RecipeViewData.Data viewData, ItemSheet.Row itemRow, int count = 0)
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

            levelBg.gameObject.SetActive(true);
            var sheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
            var currentAvatarLevel = States.Instance.CurrentAvatarState.level;
            var requirementLevel = sheet.TryGetValue(itemRow.Id, out var row) ? row.Level : 1;

            if (currentAvatarLevel >= requirementLevel)
            {
                normalLevelText.text = requirementLevel.ToString();
                normalLevelText.gameObject.SetActive(true);
                lockedLevelText.gameObject.SetActive(false);
            }
            else
            {
                lockedLevelText.text = requirementLevel.ToString();
                lockedLevelText.gameObject.SetActive(true);
                normalLevelText.gameObject.SetActive(false);
            }

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

            if (countText != null)
            {
                if (count > 0)
                {
                    countText.text = $"<size=70%>x</size>{count}";
                    countText.gameObject.SetActive(true);
                }
                else
                {
                    countText.gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CollectionMaterialInfo : MonoBehaviour
    {
        [Serializable]
        public struct IconArea
        {
            [Space]
            public TextMeshProUGUI itemName;
            public TooltipItemView itemView;

            [Space]
            public TextMeshProUGUI gradeText;
            public Image gradeAndSubTypeSpacer;
            public TextMeshProUGUI subTypeText;

            [Space]
            public GameObject skillRequiredObject;
            public TextMeshProUGUI requiredAmountText;
            public TextMeshProUGUI currentAmountText;

            [Space]
            public GameObject elementalTypeObject;
            public Image elementalTypeImage;
            public TextMeshProUGUI elementalTypeText;

            [Space]
            public TextMeshProUGUI itemDescriptionText;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private IconArea iconArea;

        [SerializeField]
        private AcquisitionPlaceButton[] acquisitionPlaceButtons;

        private const int MaxCountOfAcquisitionPlace = 3;

        public IObservable<Unit> OnClickCloseButton => closeButton.OnClickAsObservable();

        public void Show(
            Widget shortcutCaller,
            CollectionMaterial collectionMaterial,
            bool required)
        {
            var row = collectionMaterial.Row;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            if (!itemSheet.TryGetValue(row.ItemId, out var itemRow))
            {
                return;
            }

            gameObject.SetActive(true);

            iconArea.itemName.text = itemRow.GetLocalizedName(row.Level);
            iconArea.itemView.Set(itemRow, row);

            var (gradeColor, gradeText, subTypeText) = itemRow.GetGradeData();
            iconArea.gradeText.text = gradeText;
            iconArea.gradeText.color = gradeColor;
            iconArea.subTypeText.text = subTypeText;
            iconArea.subTypeText.color = gradeColor;
            iconArea.gradeAndSubTypeSpacer.color = gradeColor;

            iconArea.skillRequiredObject.SetActive(row.SkillContains);
            var isOnRequiredAmount = row.Count > 1 || row.Level > 0;
            iconArea.requiredAmountText.gameObject.SetActive(isOnRequiredAmount);
            iconArea.currentAmountText.gameObject.SetActive(isOnRequiredAmount && collectionMaterial.HasItem);
            var levelRequired = row.Level > 1;
            if (levelRequired)
            {
                iconArea.requiredAmountText.text = L10nManager.Localize("UI_REQUIRED_LEVEL_FORMAT", $"+{row.Level}");
                iconArea.currentAmountText.text = L10nManager.Localize("UI_CURRENT_ITEM_LEVEL_FORMAT", $"+{collectionMaterial.CurrentAmount}");
            }
            else
            {
                iconArea.requiredAmountText.text = L10nManager.Localize("UI_REQUIRED_COUNT_FORMAT", row.Count);
                iconArea.currentAmountText.text = L10nManager.Localize("UI_CURRENT_ITEM_COUNT_FORMAT", collectionMaterial.CurrentAmount);
            }

            if (itemRow.ItemType.HasElementType())
            {
                iconArea.elementalTypeText.text = itemRow.ElementalType.GetLocalizedString();
                iconArea.elementalTypeText.color = itemRow.ElementalType.GetElementalTypeColor();
                var sprite = itemRow.ElementalType.GetSprite();
                if (sprite is not null)
                {
                    iconArea.elementalTypeImage.overrideSprite = sprite;
                    iconArea.elementalTypeObject.SetActive(true);
                }
            }
            else
            {
                iconArea.elementalTypeObject.SetActive(false);
            }

            iconArea.itemDescriptionText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{itemRow.Id}");

            SetAcquisitionPlaceButtons(shortcutCaller, itemRow, required);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void SetAcquisitionPlaceButtons(
            Widget shortcutCaller,
            ItemSheet.Row row,
            bool required)
        {
            foreach (var button in acquisitionPlaceButtons)
            {
                button.gameObject.SetActive(false);
            }

            var acquisitionPlaceList = ShortcutHelper.GetAcquisitionPlaceList(shortcutCaller, row, required);
            if (acquisitionPlaceList.Any())
            {
                var repeatCount = Math.Min(acquisitionPlaceList.Count, MaxCountOfAcquisitionPlace);
                for (var i = 0; i < repeatCount; i++)
                {
                    acquisitionPlaceButtons[i].gameObject.SetActive(true);
                    acquisitionPlaceButtons[i].Set(acquisitionPlaceList[i]);
                }
            }
        }
    }
}

using System;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using TMPro;
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
            public TextMeshProUGUI countText;

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

        private const int MaxCountOfAcquisitionPlace = 4;

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
        }

        public void Show(CollectionSheet.RequiredMaterial material)
        {
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            if (!itemSheet.TryGetValue(material.ItemId, out var row))
            {
                return;
            }

            iconArea.itemName.text = row.GetLocalizedName(material.Level);
            iconArea.itemView.Set(row, material);

            var (gradeColor, gradeText, subTypeText) = row.GetGradeData();
            iconArea.gradeText.text = gradeText;
            iconArea.gradeText.color = gradeColor;
            iconArea.subTypeText.text = subTypeText;
            iconArea.subTypeText.color = gradeColor;
            iconArea.gradeAndSubTypeSpacer.color = gradeColor;

            iconArea.skillRequiredObject.SetActive(material.SkillContains);
            iconArea.countText.gameObject.SetActive(material.Count > 1);
            if (material.Count > 1)
            {
                iconArea.countText.text = L10nManager.Localize("UI_REQUIRED_COUNT_FORMAT", material.Count);
            }

            if (row.ItemType.HasElementType())
            {
                iconArea.elementalTypeText.text = row.ElementalType.GetLocalizedString();
                iconArea.elementalTypeText.color = row.ElementalType.GetElementalTypeColor();
                var sprite = row.ElementalType.GetSprite();
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

            iconArea.itemDescriptionText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{row.Id}");

            // SetAcquisitionPlaceButtons(itemBase);

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void SetAcquisitionPlaceButtons(ItemBase itemBase)
        {
            foreach (var button in acquisitionPlaceButtons)
            {
                button.gameObject.SetActive(false);
            }

            // Todo: Implement
            // var acquisitionPlaceList = ShortcutHelper.GetAcquisitionPlaceList(, itemBase);
            // if (acquisitionPlaceList.Any())
            // {
            //     var repeatCount = Math.Min(acquisitionPlaceList.Count, MaxCountOfAcquisitionPlace);
            //     for (var i = 0; i < repeatCount; i++)
            //     {
            //         acquisitionPlaceButtons[i].gameObject.SetActive(true);
            //         acquisitionPlaceButtons[i].Set(acquisitionPlaceList[i]);
            //     }
            // }
        }
    }
}

using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
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

            [Space]
            public GameObject elementalTypeObject;
            public Image elementalTypeImage;
            public TextMeshProUGUI elementalTypeText;

            [Space]
            public GameObject countObject;
            public TextMeshProUGUI countText;

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

        public void Show(ItemBase itemBase, int itemCount, bool levelLimit, bool skillRequired)
        {
            iconArea.itemName.text = itemBase.GetLocalizedName(false);
            iconArea.itemView.Set(itemBase, itemCount, levelLimit);

            var gradeColor = itemBase.GetItemGradeColor();
            iconArea.gradeText.text = itemBase.GetGradeText();
            iconArea.gradeText.color = gradeColor;
            iconArea.subTypeText.text = itemBase.GetSubTypeText();
            iconArea.subTypeText.color = gradeColor;
            iconArea.gradeAndSubTypeSpacer.color = gradeColor;

            iconArea.skillRequiredObject.SetActive(skillRequired);

            var sprite = itemBase.ElementalType.GetSprite();
            if (sprite is null || !itemBase.ItemType.HasElementType())
            {
                iconArea.elementalTypeObject.SetActive(false);
                return;
            }

            iconArea.elementalTypeText.text = itemBase.ElementalType.GetLocalizedString();
            iconArea.elementalTypeText.color = itemBase.GetElementalTypeColor();
            iconArea.elementalTypeImage.overrideSprite = sprite;
            iconArea.elementalTypeObject.SetActive(true);

            if (itemBase.ItemType == ItemType.Material)
            {
                iconArea.countText.text = L10nManager.Localize("UI_COUNT_FORMAT", itemCount);
                iconArea.countObject.SetActive(itemCount > 0);
            }

            iconArea.itemDescriptionText.text = itemBase.GetLocalizedDescription();

            SetAcquisitionPlaceButtons(itemBase);
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

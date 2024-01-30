using System;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CollectionCell : RectCell<Collection.Model, CollectionScroll.ContextModel>
    {
        [Serializable]
        public class CellBackground
        {
            public GameObject container;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI[] statTexts;
        }

        [SerializeField] private CellBackground complete;
        [SerializeField] private CellBackground incomplete;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton activeButton;

        public override void UpdateContent(Collection.Model itemData)
        {
            activeButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Context.OnClickActiveButton.OnNext(itemData);
            }).AddTo(gameObject);

            complete.container.SetActive(false);
            incomplete.container.SetActive(false);

            var cellBackground = itemData.Active ? complete : incomplete;
            cellBackground.container.SetActive(true);
            cellBackground.nameText.text =
                L10nManager.Localize($"COLLECTION_NAME_{itemData.Row.Id}");
            for (var i = 0; i < cellBackground.statTexts.Length; i++)
            {
                var statText = cellBackground.statTexts[i];
                statText.gameObject.SetActive(i < itemData.Row.StatModifiers.Count);
                if (i < itemData.Row.StatModifiers.Count)
                {
                    var statModifier = itemData.Row.StatModifiers[i];
                    statText.text = $"{statModifier.StatType} {statModifier.StatType.ValueToString(statModifier.Value)}";
                }
            }

            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                var itemView = collectionItemViews[i];
                itemView.gameObject.SetActive(i < itemData.Row.Materials.Count);
                if (i < itemData.Row.Materials.Count)
                {
                    var material = itemData.Row.Materials[i];
                    var itemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
                    var item = new Weapon(itemSheet[material.ItemId], Guid.NewGuid(), 0);
                    itemView.Set(item, material.Level, material.Count);
                }
            }

            // set conditional

            activeButton.Interactable = !itemData.Active;
        }
    }
}

using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CollectionCell : RectCell<Collection.Model, CollectionScroll.ContextModel>
    {
        [SerializeField] private CollectionStat complete;
        [SerializeField] private CollectionStat incomplete;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton activeButton;
        [SerializeField] private GameObject activeButtonLoadingObject;

        private Collection.Model _itemData;

        private void Awake()
        {
            LoadingHelper.ActivateCollection
                .Subscribe(activeButtonLoadingObject.SetActive)
                .AddTo(gameObject);

            activeButton.OnSubmitSubject
                .Subscribe(_=> Context.OnClickActiveButton.OnNext(_itemData))
                .AddTo(gameObject);
        }

        public override void UpdateContent(Collection.Model itemData)
        {
            _itemData = itemData;

            complete.gameObject.SetActive(false);
            incomplete.gameObject.SetActive(false);

            var cellBackground = itemData.Active ? complete : incomplete;
            cellBackground.Set(itemData);

            var materialCount = itemData.Row.Materials.Count;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;

            var canActive = true;
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                var material = itemData.Row.Materials[i];
                var itemRow = itemSheet[material.ItemId];

                CollectionMaterial requiredItem;
                if (itemData.Active)
                {
                    requiredItem = new CollectionMaterial(material, itemRow.Grade, itemRow.ItemType);
                }
                else
                {
                    var items = inventory.Items.Where(item => item.item.Id == material.ItemId).ToArray();
                    var hasItem = items.Any();

                    bool enoughCount;
                    switch (itemRow.ItemType)
                    {
                        case ItemType.Equipment:
                            enoughCount = !hasItem || items
                                .Select(item => item.item).OfType<Equipment>()
                                .Any(item => item.level == material.Level);
                            break;
                        case ItemType.Material:
                        case ItemType.Consumable:
                            enoughCount = items.Length > material.Count || !hasItem;
                            break;
                        default:
                            enoughCount = hasItem;
                            break;
                    }

                    requiredItem = new CollectionMaterial(
                        material, itemRow.Grade, hasItem, itemRow.ItemType, enoughCount);
                }

                collectionItemViews[i].Set(
                    requiredItem,
                    model => Context.OnClickMaterial.OnNext(model));

                canActive &= requiredItem.Enough;
            }

            activeButton.SetCondition(() => canActive);
            activeButton.Interactable = !itemData.Active;
        }
    }
}

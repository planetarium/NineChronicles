using System.Linq;
using Nekoyume.Game.Controller;
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

        public override void UpdateContent(Collection.Model itemData)
        {
            activeButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Context.OnClickActiveButton.OnNext(itemData);
            }).AddTo(gameObject);

            complete.gameObject.SetActive(false);
            incomplete.gameObject.SetActive(false);

            var cellBackground = itemData.Active ? complete : incomplete;
            cellBackground.Set(itemData);

            var materialCount = itemData.Row.Materials.Count;
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                var material = itemData.Row.Materials[i];
                var itemRow = itemSheet[material.ItemId];
                var items = inventory.Items.Where(item => item.item.Id == material.ItemId).ToArray();
                var equipments = items.Select(item => item.item).OfType<Equipment>().ToArray();

                collectionItemViews[i].Set(
                    new CollectionMaterial(
                        itemRow, items.Any(),
                        material.Level, equipments.Any() ||
                                        equipments.Any(item => item.level == material.Level),
                        material.Count, items.Any() || items.Length > material.Count),
                    model => Context.OnClickMaterial.OnNext(model));
            }

            // set conditional of active button

            activeButton.Interactable = !itemData.Active;
        }
    }
}

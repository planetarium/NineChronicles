using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Module
{
    public class RequiredItemRecipeView : MonoBehaviour
    {
        [SerializeField]
        private RequiredItemView[] requiredItemViews = null;

        [SerializeField]
        private Image plusImage = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetData(
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materials,
            bool checkInventory
        )
        {
            requiredItemViews[0].gameObject.SetActive(true);
            SetView(requiredItemViews[0], baseMaterialInfo.Id, baseMaterialInfo.Count, checkInventory);

            for (int i = 1; i < requiredItemViews.Length; ++i)
            {
                if (i - 1 >= materials.Count)
                {
                    requiredItemViews[i].gameObject.SetActive(false);
                }
                else
                {
                    SetView(requiredItemViews[i], materials[i - 1].Id, materials[i - 1].Count, checkInventory);
                    requiredItemViews[i].gameObject.SetActive(true);
                }
            }

            Show();
        }

        private void SetView(
            RequiredItemView view,
            int materialId,
            int requiredCount,
            bool checkInventory
        )
        {
            var item = ItemFactory.CreateMaterial(Game.Game.instance.TableSheets.MaterialItemSheet, materialId);
            var itemCount = requiredCount;
            if (checkInventory)
            {
                var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
                itemCount = inventory.TryGetFungibleItem(item, out var inventoryItem)
                    ? inventoryItem.count
                    : 0;
            }
            var countableItem = new CountableItem(item, itemCount);
            view.SetData(countableItem, requiredCount);
        }
    }
}

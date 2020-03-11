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

        public void SetData(EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo, List<EquipmentItemSubRecipeSheet.MaterialInfo> materials)
        {
            requiredItemViews[0].gameObject.SetActive(true);
            SetView(requiredItemViews[0], baseMaterialInfo.Id, baseMaterialInfo.Count);

            for (int i = 1; i < requiredItemViews.Length; ++i)
            {
                if (i - 1 >= materials.Count)
                {
                    requiredItemViews[i].gameObject.SetActive(false);
                }
                else
                {
                    SetView(requiredItemViews[i], materials[i - 1].Id, materials[i - 1].Count);
                    requiredItemViews[i].gameObject.SetActive(true);
                }
            }

            Show();
        }

        private void SetView(RequiredItemView view, int materialId, int requiredCount)
        {
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;

            var item = ItemFactory.CreateMaterial(Game.Game.instance.TableSheets.MaterialItemSheet, materialId);

            if (inventory.TryGetFungibleItem(item, out var inventoryItem))
            {
                var countableItem = new CountableItem(item, inventoryItem.count);
                view.SetData(countableItem, requiredCount);
            }
            else
            {
                var countableItem = new CountableItem(item, 0);
                view.SetData(countableItem, requiredCount);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Module;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.TableData;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Scroller
{
    class RecipeCell : MonoBehaviour
    {
        [SerializeField] private RecipeViewData recipeViewData = null;
        [SerializeField] private RecipeView equipmentView = null;
        [SerializeField] private RecipeView consumableView = null;
        [SerializeField] private Image selectedImage = null;
        [SerializeField] private GameObject lockObject = null;

        public void Show(ItemSheet.Row itemRow)
        {
            var viewData = recipeViewData.GetData(itemRow.Grade);

            if (itemRow.ItemType == ItemType.Equipment)
            {
                equipmentView.Show(viewData, itemRow);
                consumableView.Hide();
            }
            else if (itemRow.ItemType == ItemType.Consumable)
            {
                equipmentView.Hide();
                consumableView.Show(viewData, itemRow);
            }
            else
            {
                Debug.LogError($"Recipe view of {itemRow.ItemType} is not supported.");
            }

            lockObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Lock()
        {
            equipmentView.Hide();
            consumableView.Hide();
            lockObject.SetActive(true);
        }
    }
}

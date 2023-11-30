using System.Linq;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SummonDetailPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SummonDetailScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
            CloseWidget = closeButton.onClick.Invoke;
        }

        public void Show(SummonSheet.Row summonRow)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;

            float ratioSum = summonRow.Recipes.Sum(pair => pair.Item2);
            var models = summonRow.Recipes.Where(pair => pair.Item1 > 0).Select(pair =>
            {
                var (recipeId, ratio) = pair;
                if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
                {
                    return null;
                }
                return new SummonDetailCell.Model
                {
                    EquipmentRow = equipmentItemSheet[recipeRow.ResultEquipmentId],
                    Ratio = ratio / ratioSum,
                };
            }).OrderBy(model => model.Ratio);

            scroll.UpdateData(models, true);

            base.Show();
        }
    }
}

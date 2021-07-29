using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Craft : Widget
    {
        [SerializeField] private Button closeButton = null;

        [SerializeField] private RecipeScroll recipeScroll = null;

        private Dictionary<ItemSubType, Dictionary<string, RecipeRow.Model>> _models;

        private const string EquipmentSplitFormat = "{0}_{1}";

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close(true));
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (_models is null)
            {
                LoadRecipeModel();
            }

            recipeScroll.Show(_models[ItemSubType.Weapon].Values, true);
            base.Show(ignoreShowAnimation);
        }

        private void LoadRecipeModel()
        {
            _models = new Dictionary<ItemSubType, Dictionary<string, RecipeRow.Model>>();
            var tableSheets = Game.Game.instance.TableSheets;

            var recipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var equipmentIds = recipeSheet.Values
                .Select(r => r.ResultEquipmentId);
            var equipments = tableSheets.EquipmentItemSheet.Values
                .Where(r => equipmentIds.Contains(r.Id));

            foreach (var equipment in equipments)
            {
                var itemSubType = equipment.ItemSubType;
                if (!_models.ContainsKey(itemSubType))
                {
                    _models[itemSubType] = new Dictionary<string, RecipeRow.Model>();
                }

                var idString = equipment.Id.ToString();
                var tierArea = idString.Substring(0, 4);
                var variationArea = idString.Substring(5);
                var key = string.Format(EquipmentSplitFormat, tierArea, variationArea);

                if (!_models[itemSubType].TryGetValue(key, out var model))
                {
                    model = new RecipeRow.Model(
                        equipment.GetLocalizedName(),
                        equipment.Grade
                        );
                    _models[itemSubType][key] = model;
                }

                model.Rows.Add(equipment);
            }
        }
    }
}

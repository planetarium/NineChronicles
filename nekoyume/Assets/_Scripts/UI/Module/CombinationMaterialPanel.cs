using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialPanel : MonoBehaviour
    {
        public TextMeshProUGUI materialText;
        public RequiredItemView[] materialViews;
        public decimal costNcg;
        public int costAp;

        public List<(Nekoyume.Model.Item.Material, int)> MaterialList { get; private set; }
        public bool IsCraftable { get; set; }

        private void Awake()
        {
            materialText.text = LocalizationManager.Localize("UI_MATERIALS");
        }

        public void SetData(EquipmentItemRecipeSheet.Row row)
        {
            MaterialList = new List<(Nekoyume.Model.Item.Material, int)>();
            costNcg = 0m;
            costAp = 0;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var materialRow = materialSheet.Values.First(i => i.Id == row.MaterialId);
            var baseMaterial = (Nekoyume.Model.Item.Material) ItemFactory.Create(materialRow, Guid.Empty);
            MaterialList.Add((baseMaterial, row.MaterialCount));
            costNcg += row.RequiredGold;
            costAp += row.RequiredActionPoint;
            // 서브 레시피 아이디도 선택이 되야함
            if (row.SubRecipeIds.Any())
            {
                var subRecipeId = row.SubRecipeIds.First();
                var subRecipeSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                var subRecipeRow = subRecipeSheet.Values.First(i => i.Id == subRecipeId);
                foreach (var materialInfo in subRecipeRow.Materials)
                {
                    var subMaterialRow = materialSheet.Values.First(i => i.Id == materialInfo.Id);
                    var subMaterial = (Nekoyume.Model.Item.Material) ItemFactory.Create(subMaterialRow, Guid.Empty);
                    MaterialList.Add((subMaterial, materialInfo.Count));
                }

                costNcg += subRecipeRow.RequiredGold;
                costAp += subRecipeRow.RequiredActionPoint;
            }

            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            IsCraftable = true;

            for (var index = 0; index < materialViews.Length; index++)
            {
                var view = materialViews[index];
                view.gameObject.SetActive(false);
                if (index < MaterialList.Count)
                {
                    var (material, requiredCount) = MaterialList[index];
                    inventory.TryGetFungibleItem(material, out var inventoryItem);
                    var item = new CountableItem(material, inventoryItem?.count ?? 0);
                    view.SetData(item, requiredCount);
                    view.gameObject.SetActive(true);

                    if (item.Count.Value < requiredCount)
                        IsCraftable = false;
                }
            }
        }
    }
}

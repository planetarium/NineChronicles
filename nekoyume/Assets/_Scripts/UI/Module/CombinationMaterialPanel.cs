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
        public decimal costNCG;
        public int costAP;

        public List<(Nekoyume.Model.Item.Material, int)> MaterialList { get; private set; }
        public bool IsCraftable { get; set; }

        private void Awake()
        {
            materialText.text = LocalizationManager.Localize("UI_MATERIALS");
        }

        public virtual void SetData(
            EquipmentItemRecipeSheet.Row row,
            int? subRecipeId,
            bool checkInventory = true
        )
        {
            MaterialList = new List<(Nekoyume.Model.Item.Material, int)>();
            costNCG = 0m;
            costAP = 0;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var materialRow = materialSheet.Values.First(i => i.Id == row.MaterialId);
            var baseMaterial = ItemFactory.CreateMaterial(materialRow);
            MaterialList.Add((baseMaterial, row.MaterialCount));
            costNCG += row.RequiredGold;
            costAP += row.RequiredActionPoint;

            if (subRecipeId.HasValue)
            {
                var subRecipeSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                var subRecipeRow = subRecipeSheet.Values.First(i => i.Id == subRecipeId);
                foreach (var materialInfo in subRecipeRow.Materials)
                {
                    var subMaterialRow = materialSheet.Values.First(i => i.Id == materialInfo.Id);
                    var subMaterial = ItemFactory.CreateMaterial(subMaterialRow);
                    MaterialList.Add((subMaterial, materialInfo.Count));
                }

                costNCG += subRecipeRow.RequiredGold;
                costAP += subRecipeRow.RequiredActionPoint;
            }

            IsCraftable = Widget.Find<Combination>().selectedIndex >= 0;
            UpdateMaterialViews(checkInventory);
        }

        public void SetData(ConsumableItemRecipeSheet.Row row, bool checkInventory = true, bool canCraft = false)
        {
            MaterialList = new List<(Nekoyume.Model.Item.Material, int)>();
            costNCG = 0m;
            costAP = 0;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            foreach (var materialId in row.MaterialItemIds)
            {
                var materialRow = materialSheet.Values.First(i => i.Id == materialId);
                var baseMaterial = ItemFactory.CreateMaterial(materialRow);
                MaterialList.Add((baseMaterial, 1));
                costNCG += row.RequiredGold;
                costAP += row.RequiredActionPoint;
            }

            IsCraftable = canCraft;
            UpdateMaterialViews(checkInventory);
        }

        private void UpdateMaterialViews(bool checkInventory)
        {
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            for (var index = 0; index < materialViews.Length; index++)
            {
                var view = materialViews[index];
                view.gameObject.SetActive(false);
                if (index < MaterialList.Count)
                {
                    var (material, requiredCount) = MaterialList[index];
                    var itemCount = requiredCount;
                    if (checkInventory)
                    {
                        itemCount = inventory.TryGetFungibleItem(material, out var inventoryItem)
                            ? inventoryItem.count
                            : 0;
                    }
                    var item = new CountableItem(material, itemCount);
                    view.SetData(item, requiredCount);
                    if (!checkInventory)
                    {
                        view.SetRequiredText();
                    }
                    view.gameObject.SetActive(true);
                    if (item.Count.Value < requiredCount)
                    {
                        IsCraftable = false;
                    }
                }
            }
        }
    }
}

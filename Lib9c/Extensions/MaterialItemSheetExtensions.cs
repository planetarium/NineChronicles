using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Extensions
{
    public static class MaterialItemSheetExtensions
    {
        public static void ValidateFromAction(
            this MaterialItemSheet materialItemSheet,
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialInfos,
            Dictionary<int, int> requiredFungibleItems,
            string addressesHex)
        {
            for (var i = materialInfos.Count; i > 0; i--)
            {
                var materialInfo = materialInfos[i - 1];
                if (!materialItemSheet.TryGetValue(materialInfo.Id, out var materialRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(MaterialItemSheet),
                        materialInfo.Id);
                }

                if (requiredFungibleItems.ContainsKey(materialRow.Id))
                {
                    requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                }
                else
                {
                    requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                }
            }
        }
    }
}

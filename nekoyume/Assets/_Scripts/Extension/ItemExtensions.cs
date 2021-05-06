using System;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace _Scripts.Extension
{
    public static class ItemExtensions
    {
        public static bool TryParseAsTradableId(this int rowId, ItemSheet itemSheet, out Guid tradableId)
        {
            var itemRow = itemSheet.OrderedList.FirstOrDefault(e => e.Id == rowId);
            if (itemRow is null ||
                !(itemRow is MaterialItemSheet.Row materialRow))
            {
                return false;
            }

            tradableId = TradableMaterial.DeriveTradableId(materialRow.ItemId);
            return true;
        }
    }
}
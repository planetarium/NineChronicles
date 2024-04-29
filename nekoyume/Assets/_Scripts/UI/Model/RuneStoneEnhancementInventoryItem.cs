using System.Collections.Generic;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

namespace Nekoyume.UI.Model
{
    public class RuneStoneEnhancementInventoryItem
    {
        public RuneState State;
        public RuneListSheet.Row SheetData;
        public RuneItem item;
        public RuneStoneEnhancementInventoryItem(RuneState runeState, RuneListSheet.Row rowData, RuneItem runeitem)
        {
            State = runeState;
            SheetData = rowData;
            item = runeitem;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class SynthesizeModel
    {
        public int InventoryItemCount { get; set; }
        public int NeedItemCount { get; }

        public SynthesizeModel(int inventoryItemCount, int needItemCount)
        {
            InventoryItemCount = inventoryItemCount;
            NeedItemCount = needItemCount;
        }
    }
}

using System.Collections.Generic;
using Libplanet.Assets;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RuneInventoryItem : IItemViewModel
    {
        public RectTransform View { get; set; }

        public List<FungibleAssetValue> Runes { get; }
        public int BoosId { get; }

        public RuneInventoryItem(List<FungibleAssetValue> runes, int boosId)
        {
            Runes = runes;
            BoosId = boosId;
        }
    }
}

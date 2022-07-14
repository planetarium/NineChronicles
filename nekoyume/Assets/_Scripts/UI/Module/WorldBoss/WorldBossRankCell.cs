using System;
using System.Globalization;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    [Serializable]
    public class WorldBossRankItemData
    {
        public string name;
        public int rank;
        public int fullCostumeOrArmorId;
        public int cp;
        public int bestRecord;
        public int seasonTotalPoint;
    }

    public class WorldBossRankScrollContext : FancyScrollRectContext
    {
        public int selectedIndex = -1;
    }

    public class WorldBossRankCell
        : FancyScrollRectCell<WorldBossRankItemData, WorldBossRankScrollContext>
    {
        public override void UpdateContent(WorldBossRankItemData itemData)
        {

        }
    }
}

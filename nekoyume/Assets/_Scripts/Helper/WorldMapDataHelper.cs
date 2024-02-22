using System.Linq;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class WorldMapDataHelper
    {
        private static WorldMapData _worldMapData;

        private static WorldMapData WorldMapData
        {
            get
            {
                if (_worldMapData == null)
                {
                    _worldMapData = Resources.Load<WorldMapData>(
                        "ScriptableObject/WorldMapStageData");
                }

                return _worldMapData;
            }
        }

        public static WorldMapData.StageIcon GetStageIcon(EnumType.BossType bossType, EnumType.EventType eventType)
        {
            return WorldMapData.bossStageIcons.FirstOrDefault(icon => icon.bossType == bossType) ??
                   WorldMapData.eventStageIcons.FirstOrDefault(icon => icon.eventType == eventType) ??
                   WorldMapData.defaultStageIcon;
        }

        public static WorldMapData.Icon GetBossMarkIcon(EnumType.BossType bossType)
        {
            return WorldMapData.bossMarkIcons.FirstOrDefault(icon => icon.bossType == bossType)?.icon;
        }
    }
}

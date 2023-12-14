using System;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class PromotionContainer : MonoBehaviour
    {
        [Serializable]
        private enum Season
        {
            None,
            Arena,
            WorldBoss
        }

        [Serializable]
        private class Time
        {
            public string beginDateTime;
            public string endDateTime;
        }

        [SerializeField]
        private Season season;

        [SerializeField]
        private Time[] times;

        private void OnEnable()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var inSeason = false;
            switch (season)
            {
                case Season.Arena:
                    var arenaSheet = Game.Game.instance.TableSheets.ArenaSheet;
                    var arenaRoundData = arenaSheet.GetRoundByBlockIndex(blockIndex);
                    inSeason = arenaRoundData.ArenaType == ArenaType.Season;
                    break;
                case Season.WorldBoss:
                    var worldBossStatus = WorldBossFrontHelper.GetStatus(blockIndex);
                    inSeason = worldBossStatus == WorldBossStatus.Season;
                    break;
            }

            var isInTime = false;
            foreach (var time in times)
            {
                isInTime |= DateTime.UtcNow.IsInTime(time.beginDateTime, time.endDateTime);
            }

            gameObject.SetActive(inSeason || isInTime);
        }
    }
}

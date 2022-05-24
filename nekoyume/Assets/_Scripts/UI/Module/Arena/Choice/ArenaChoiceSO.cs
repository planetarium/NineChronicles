using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena
{
    [CreateAssetMenu(
        fileName = "ArenaJoinSO",
        menuName = "Scriptable Object/ArenaJoinSO",
        order = 0)]
    [Serializable]
    public class ArenaJoinSO : ScriptableObject
    {
        [SerializeField]
        private List<ArenaJoinSeasonItemData> _scrollData;

        public IList<ArenaJoinSeasonItemData> ScrollData => _scrollData;

        [SerializeField]
        private int _medalId;

        public int MedalId => _medalId;
        
        [SerializeField]
        private int _conditionMax;

        [SerializeField]
        private int _conditionCurrent;

        public (int max, int current) Conditions => (_conditionMax, _conditionCurrent);

        [SerializeField]
        private ArenaJoinSeasonInfo.RewardType _rewardType;

        public ArenaJoinSeasonInfo.RewardType RewardType => _rewardType;

        public ArenaJoinSO()
        {
            _scrollData = new List<ArenaJoinSeasonItemData>
            {
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Offseason,
                    name = "Offseason #1"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Season,
                    name = "Season #1"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Offseason,
                    name = "Offseason #2"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Championship,
                    name = "Championship #1"
                },
            };

            _medalId = 700000;
            _conditionMax = 100;
            _conditionCurrent = 0;
            _rewardType = ArenaJoinSeasonInfo.RewardType.Medal;
        }
    }
}

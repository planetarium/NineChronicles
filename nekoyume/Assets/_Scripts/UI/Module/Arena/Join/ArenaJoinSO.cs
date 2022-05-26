using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Join
{
    [CreateAssetMenu(
        fileName = "ArenaJoinSO",
        menuName = "Scriptable Object/ArenaJoinSO",
        order = 0)]
    [Serializable]
    public class ArenaJoinSO : ScriptableObject
    {
        [Serializable]
        public class ArenaData
        {
            public ArenaJoinSeasonItemData ItemData;
            public ArenaJoinSeasonInfo.RewardType RewardType;
        }

        [SerializeField]
        private List<ArenaData> _arenaDataList;

        public IList<ArenaData> ArenaDataList => _arenaDataList;

        [SerializeField]
        private int _medalId;

        public int MedalId => _medalId;

        [SerializeField]
        private int _conditionMax;

        [SerializeField]
        private int _conditionCurrent;

        public (int max, int current) Conditions => (_conditionMax, _conditionCurrent);

        public ArenaJoinSO()
        {
            _arenaDataList = new List<ArenaData>
            {
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Offseason,
                        text = string.Empty,
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Season,
                        text = "1",
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Offseason,
                        text = string.Empty,
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Season,
                        text = "2",
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Offseason,
                        text = string.Empty,
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Season,
                        text = "3",
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Offseason,
                        text = string.Empty,
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    ItemData = new ArenaJoinSeasonItemData
                    {
                        type = ArenaJoinSeasonType.Championship,
                        text = "1",
                    },
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG |
                                 ArenaJoinSeasonInfo.RewardType.Costume,
                },
            };

            _medalId = 700000;
            _conditionMax = 100;
            _conditionCurrent = 0;
        }
    }
}

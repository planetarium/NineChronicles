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

        public ArenaJoinSO()
        {
            _scrollData = new List<ArenaJoinSeasonItemData>
            {
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Preseason,
                    name = "Weekly #1"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Season,
                    name = "Monthly #1"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Preseason,
                    name = "Weekly #2"
                },
                new ArenaJoinSeasonItemData
                {
                    type = ArenaJoinSeasonType.Championship,
                    name = "Quarterly #1"
                },
            };
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Board
{
    [CreateAssetMenu(
        fileName = "ArenaBoardSO",
        menuName = "Scriptable Object/ArenaBoardSO",
        order = 0)]
    [Serializable]
    public class ArenaBoardSO : ScriptableObject
    {
        [SerializeField]
        private List<ArenaBoardPlayerItemData> _arenaBoardPlayerScrollData;

        public IList<ArenaBoardPlayerItemData> ArenaBoardPlayerScrollData =>
            _arenaBoardPlayerScrollData;

        [SerializeField]
        private string _seasonText;

        public string SeasonText => _seasonText;
        
        [SerializeField]
        private int _rank;

        public int Rank => _rank;

        [SerializeField]
        private int _winCount;

        public int WinCount => _winCount;

        [SerializeField]
        private int _loseCount;

        public int LoseCount => _loseCount;

        [SerializeField]
        private int _cp;

        public int CP => _cp;

        [SerializeField]
        private int _rating;

        public int Rating => _rating;

        public ArenaBoardSO()
        {
            _arenaBoardPlayerScrollData = new List<ArenaBoardPlayerItemData>
            {
                new ArenaBoardPlayerItemData
                {
                    name = "Alpha",
                    cp = "999,999",
                    rating = "999,999",
                    plusRating = "999",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Bravo",
                    cp = "999,998",
                    rating = "999,998",
                    plusRating = "998",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Charlie",
                    cp = "999,997",
                    rating = "999,997",
                    plusRating = "997",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Delta",
                    cp = "999,996",
                    rating = "999,996",
                    plusRating = "996",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Echo",
                    cp = "999,995",
                    rating = "999,995",
                    plusRating = "995",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Foxtrot",
                    cp = "999,994",
                    rating = "999,994",
                    plusRating = "994",
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Golf",
                    cp = "999,993",
                    rating = "999,993",
                    plusRating = "993",
                },
            };

            _seasonText = "offseason";
            _rank = 999_999;
            _winCount = 999;
            _loseCount = 999;
            _cp = 999_999;
            _rating = 999_999;
        }
    }
}

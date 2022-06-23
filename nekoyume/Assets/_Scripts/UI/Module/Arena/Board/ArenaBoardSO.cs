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
        [SerializeField] private List<ArenaBoardPlayerItemData> _arenaBoardPlayerScrollData;

        public List<ArenaBoardPlayerItemData> ArenaBoardPlayerScrollData =>
            _arenaBoardPlayerScrollData;

        [SerializeField] private string _seasonText;

        public string SeasonText => _seasonText;

        [SerializeField] private int _rank;

        public int Rank => _rank;

        [SerializeField] private int _winCount;

        public int WinCount => _winCount;

        [SerializeField] private int _loseCount;

        public int LoseCount => _loseCount;

        [SerializeField] private int _cp;

        public int CP => _cp;

        [SerializeField] private int _rating;

        public int Rating => _rating;

        public ArenaBoardSO()
        {
            _arenaBoardPlayerScrollData = new List<ArenaBoardPlayerItemData>
            {
                new ArenaBoardPlayerItemData
                {
                    name = "Alpha",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_999,
                    score = 999_999,
                    rank = 1,
                    expectWinDeltaScore = 999,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Bravo",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_998,
                    score = 999_998,
                    rank = 2,
                    expectWinDeltaScore = 998,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Charlie",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_997,
                    score = 999_997,
                    rank = 3,
                    expectWinDeltaScore = 997,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Delta",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_996,
                    score = 999_996,
                    rank = 4,
                    expectWinDeltaScore = 996,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Echo",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_995,
                    score = 999_995,
                    rank = 5,
                    expectWinDeltaScore = 995,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Foxtrot",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_994,
                    score = 999_994,
                    rank = 6,
                    expectWinDeltaScore = 994,
                    interactableChoiceButton = true,
                },
                new ArenaBoardPlayerItemData
                {
                    name = "Golf",
                    level = 99,
                    fullCostumeOrArmorId = GameConfig.DefaultAvatarArmorId,
                    titleId = null,
                    cp = 999_993,
                    score = 999_993,
                    rank = 7,
                    expectWinDeltaScore = 993,
                    interactableChoiceButton = true,
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
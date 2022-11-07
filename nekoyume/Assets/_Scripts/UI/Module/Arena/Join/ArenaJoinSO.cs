using System;
using System.Collections.Generic;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
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
            public RoundDataBridge RoundDataBridge;
            public ArenaJoinSeasonInfo.RewardType RewardType;
        }

        [Serializable]
        public class RoundDataBridge
        {
            public int ChampionshipId;
            public int Round;
            public ArenaType ArenaType;
            public long StartBlockIndex;
            public long EndBlockIndex;
            public int RequiredMedalCount;
            public long EntranceFee;
            public long DiscountedEntranceFee;
            public long TicketPrice;
            public long AdditionalTicketPrice;
            public int MaxPurchaseCount;
            public int MaxPurchaseCountWithInterval;

            public RoundDataBridge(int championshipId, int round, ArenaType arenaType,
                long startBlockIndex, long endBlockIndex,
                int requiredMedalCount,
                long entranceFee, long discountedEntranceFee,
                long ticketPrice, long additionalTicketPrice,
                int maxPurchaseCount, int maxPurchaseCountWithInterval)
            {
                ChampionshipId = championshipId;
                Round = round;
                ArenaType = arenaType;
                StartBlockIndex = startBlockIndex;
                EndBlockIndex = endBlockIndex;
                RequiredMedalCount = requiredMedalCount;
                EntranceFee = entranceFee;
                DiscountedEntranceFee = discountedEntranceFee;
                TicketPrice = ticketPrice;
                AdditionalTicketPrice = additionalTicketPrice;
                MaxPurchaseCount = maxPurchaseCount;
                MaxPurchaseCountWithInterval = maxPurchaseCountWithInterval;
            }

            public ArenaSheet.RoundData ToRoundData() => new ArenaSheet.RoundData(
                ChampionshipId,
                Round,
                ArenaType,
                StartBlockIndex,
                EndBlockIndex,
                RequiredMedalCount,
                EntranceFee,
                TicketPrice,
                AdditionalTicketPrice,
                MaxPurchaseCount,
                MaxPurchaseCountWithInterval
            );
        }

        [SerializeField]
        private List<ArenaData> _arenaDataList;

        public IList<ArenaData> ArenaDataList => _arenaDataList;

        [SerializeField]
        private int _conditionRequired;

        [SerializeField]
        private int _conditionCurrent;

        public (int required, int current) Conditions => (_conditionRequired, _conditionCurrent);

        public ArenaJoinSO()
        {
            _arenaDataList = new List<ArenaData>
            {
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        1,
                        ArenaType.OffSeason,
                        0,
                        100,
                        0,
                        0,
                        0,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        2,
                        ArenaType.Season,
                        101,
                        200,
                        0,
                        100,
                        50,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        3,
                        ArenaType.OffSeason,
                        201,
                        300,
                        0,
                        0,
                        0,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        4,
                        ArenaType.Season,
                        301,
                        400,
                        0,
                        100,
                        50,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        5,
                        ArenaType.OffSeason,
                        401,
                        500,
                        0,
                        0,
                        0,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        6,
                        ArenaType.Season,
                        501,
                        600,
                        0,
                        100,
                        50,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        7,
                        ArenaType.OffSeason,
                        601,
                        700,
                        0,
                        0,
                        0,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food,
                },
                new ArenaData
                {
                    RoundDataBridge = new RoundDataBridge(
                        1,
                        8,
                        ArenaType.Championship,
                        701,
                        800,
                        8,
                        1000,
                        500,
                        5,
                        2,
                        40,
                        8),
                    RewardType = ArenaJoinSeasonInfo.RewardType.Food |
                                 ArenaJoinSeasonInfo.RewardType.Medal |
                                 ArenaJoinSeasonInfo.RewardType.NCG |
                                 ArenaJoinSeasonInfo.RewardType.Costume,
                },
            };

            _conditionRequired = 100;
            _conditionCurrent = 0;
        }
    }
}

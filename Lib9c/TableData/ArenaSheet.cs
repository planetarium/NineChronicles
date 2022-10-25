using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    [Serializable]
    public class ArenaSheet : Sheet<int, ArenaSheet.Row>
    {
        [Serializable]
        public class RoundData
        {
            public int ChampionshipId { get; }
            public int Round { get; }
            public ArenaType ArenaType { get; }
            public long StartBlockIndex { get; }
            public long EndBlockIndex { get; }
            public int RequiredMedalCount { get; }
            public long EntranceFee { get; }
            public long TicketPrice { get; }
            public long AdditionalTicketPrice { get; }
            public int MaxPurchaseCount { get; }
            public int MaxPurchaseCountWithInterval { get; }

            public RoundData(int championshipId,
                int round,
                ArenaType arenaType,
                long startBlockIndex,
                long endBlockIndex,
                int requiredMedalCount,
                long entranceFee,
                long ticketPrice,
                long additionalTicketPrice,
                int maxPurchaseCount,
                int maxPurchaseCountWithInterval)
            {
                ChampionshipId = championshipId;
                Round = round;
                ArenaType = arenaType;
                StartBlockIndex = startBlockIndex;
                EndBlockIndex = endBlockIndex;
                RequiredMedalCount = requiredMedalCount;
                EntranceFee = entranceFee;
                TicketPrice = ticketPrice;
                AdditionalTicketPrice = additionalTicketPrice;
                MaxPurchaseCount = maxPurchaseCount;
                MaxPurchaseCountWithInterval = maxPurchaseCountWithInterval;
            }

            public bool IsTheRoundOpened(long blockIndex)
            {
                return StartBlockIndex <= blockIndex && blockIndex <= EndBlockIndex;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => ChampionshipId;

            public int ChampionshipId { get; private set; }

            public List<RoundData> Round { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                ChampionshipId = ParseInt(fields[0]);
                var round = ParseInt(fields[1]);
                var arenaType = (ArenaType)Enum.Parse(typeof(ArenaType), fields[2]);
                var startIndex = ParseLong(fields[3]);
                var endIndex = ParseLong(fields[4]);
                var requiredWins = ParseInt(fields[5]);
                var entranceFee = ParseLong(fields[6]);
                var ticketPrice = ParseLong(fields[7]);
                var additionalTicketPrice = ParseLong(fields[8]);
                var maxPurchaseCount = 0;
                var maxPurchaseCountWithInterval = 0;
                if (fields.Count > 9)
                {
                    maxPurchaseCount = ParseInt(fields[9]);
                    maxPurchaseCountWithInterval = ParseInt(fields[10]);
                }

                Round = new List<RoundData>
                {
                    new RoundData(ChampionshipId,
                        round,
                        arenaType,
                        startIndex,
                        endIndex,
                        requiredWins,
                        entranceFee,
                        ticketPrice,
                        additionalTicketPrice,
                        maxPurchaseCount,
                        maxPurchaseCountWithInterval)
                };
            }

            public bool TryGetRound(int round, out RoundData roundData)
            {
                roundData = Round.FirstOrDefault(x => x.Round.Equals(round));
                return !(roundData is null);
            }

            public bool TryGetChampionshipRound(out RoundData roundData)
            {
                roundData = Round.FirstOrDefault(x => x.ArenaType.Equals(ArenaType.Championship));
                return !(roundData is null);
            }
        }

        public ArenaSheet() : base(nameof(ArenaSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Round.Any())
            {
                return;
            }

            row.Round.Add(value.Round[0]);
        }

        public Row GetRowByBlockIndex(long blockIndex) => OrderedList.First(e =>
            e.Round.Any(roundData => roundData.IsTheRoundOpened(blockIndex)));

        public RoundData GetRoundByBlockIndex(long blockIndex)
        {
            var roundList = OrderedList.SelectMany(row => row.Round);
            if (roundList == null || !roundList.Any())
            {
                throw new SheetRowNotFoundException(nameof(ArenaSheet), $"BlockIndex : {blockIndex}");
            }

            var round = roundList.FirstOrDefault(e => e.IsTheRoundOpened(blockIndex));
            if (round == null)
            {
                throw new RoundNotFoundException($"[{nameof(ArenaSheet)}] BlockIndex({blockIndex})");
            }

            return round;
        }
    }
}

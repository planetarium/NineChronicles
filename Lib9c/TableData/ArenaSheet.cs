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
            public long DiscountedEntranceFee { get; }
            public long TicketPrice { get; }
            public long AdditionalTicketPrice { get; }

            public RoundData(int championshipId, int round, ArenaType arenaType,
                long startBlockIndex, long endBlockIndex,
                int requiredMedalCount,
                long entranceFee, long discountedEntranceFee,
                long ticketPrice, long additionalTicketPrice)
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
                var discountedEntranceFee = ParseLong(fields[7]);
                var ticketPrice = ParseLong(fields[8]);
                var additionalTicketPrice = ParseLong(fields[9]);
                Round = new List<RoundData>
                {
                    new RoundData(ChampionshipId, round, arenaType, startIndex, endIndex,
                        requiredWins, entranceFee, discountedEntranceFee,
                        ticketPrice, additionalTicketPrice)
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

            public bool TryGetSeasonNumber(int round, out int seasonNumber)
            {
                seasonNumber = 0;
                foreach (var roundData in Round)
                {
                    if (roundData.ArenaType == ArenaType.Season)
                    {
                        seasonNumber++;
                    }

                    if (roundData.Round == round)
                    {
                        return roundData.ArenaType == ArenaType.Season;
                    }
                }

                return false;
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

        public bool TryGetRowByBlockIndex(long blockIndex, out Row row)
        {
            row = OrderedList
                .FirstOrDefault(e =>
                    e.Round.Any(roundData =>
                        roundData.IsTheRoundOpened(blockIndex)));
            return row != null;
        }

        public bool TryGetRoundByBlockIndex(long blockIndex, out RoundData roundData)
        {
            roundData = OrderedList
                .SelectMany(row => row.Round)
                .FirstOrDefault(e => e.IsTheRoundOpened(blockIndex));
            return roundData != null;
        }
    }
}

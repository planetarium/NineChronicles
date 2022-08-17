using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Event
{
    public class EventDungeonInfo : IState
    {
        public static Address DeriveAddress(Address address, int dungeonId)
        {
            return address.Derive($"event_dungeon_info_{dungeonId}");
        }

        public int ResetTicketsInterval { get; private set; }

        public int RemainingTickets { get; private set; }

        public int NumberOfTicketPurchases { get; private set; }

        public int ClearedStageId { get; private set; }

        public EventDungeonInfo(
            int resetTicketsInterval = 0,
            int remainingTickets = 0,
            int numberOfTicketPurchases = 0,
            int clearedStageId = 0)
        {
            if (resetTicketsInterval < 0)
            {
                throw new ArgumentException(
                    $"{nameof(resetTicketsInterval)} must be greater than or equal to 0.");
            }

            if (remainingTickets < 0)
            {
                throw new ArgumentException(
                    $"{nameof(remainingTickets)} must be greater than or equal to 0.");
            }

            if (numberOfTicketPurchases < 0)
            {
                throw new ArgumentException(
                    $"{nameof(numberOfTicketPurchases)} must be greater than or equal to 0.");
            }

            if (clearedStageId < 0)
            {
                throw new ArgumentException(
                    $"{nameof(clearedStageId)} must be greater than or equal to 0.");
            }

            ResetTicketsInterval = resetTicketsInterval;
            RemainingTickets = remainingTickets;
            NumberOfTicketPurchases = numberOfTicketPurchases;
            ClearedStageId = clearedStageId;
        }

        public EventDungeonInfo(Bencodex.Types.List serialized)
        {
            ResetTicketsInterval = serialized[0].ToInteger();
            RemainingTickets = serialized[1].ToInteger();
            NumberOfTicketPurchases = serialized[2].ToInteger();
            ClearedStageId = serialized[3].ToInteger();
        }

        public EventDungeonInfo(Bencodex.Types.IValue serialized)
            : this((Bencodex.Types.List)serialized)
        {
        }

        public IValue Serialize() => Bencodex.Types.List.Empty
            .Add(ResetTicketsInterval.Serialize())
            .Add(RemainingTickets.Serialize())
            .Add(NumberOfTicketPurchases.Serialize())
            .Add(ClearedStageId.Serialize());

        public void ResetTickets(int interval, int tickets)
        {
            if (interval <= ResetTicketsInterval)
            {
                throw new ArgumentException(
                    $"{nameof(interval)}({interval}) must be greater than {nameof(ResetTicketsInterval)}({ResetTicketsInterval}).");
            }

            if (tickets < 0)
            {
                throw new ArgumentException(
                    $"{nameof(tickets)} must be greater than or equal to 0.");
            }

            ResetTicketsInterval = interval;
            RemainingTickets = tickets;
        }

        public bool HasTickets(int tickets)
        {
            if (tickets < 0)
            {
                throw new ArgumentException(
                    $"{nameof(tickets)} must be greater than or equal to 0.");
            }

            return RemainingTickets >= tickets;
        }

        public bool TryUseTickets(int tickets)
        {
            if (tickets < 0)
            {
                throw new ArgumentException(
                    $"{nameof(tickets)} must be greater than or equal to 0.");
            }

            if (RemainingTickets < tickets)
            {
                return false;
            }

            RemainingTickets -= tickets;
            return true;
        }

        public void IncreaseNumberOfTicketPurchases()
        {
            if (NumberOfTicketPurchases == int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"{nameof(NumberOfTicketPurchases)}({NumberOfTicketPurchases})" +
                    $" already reached maximum value.");
            }

            NumberOfTicketPurchases++;
        }

        public void ClearStage(int stageId)
        {
            if (ClearedStageId >= stageId)
            {
                return;
            }

            ClearedStageId = stageId;
        }

        public bool IsCleared(int stageId) =>
            ClearedStageId >= stageId;

        protected bool Equals(EventDungeonInfo other) =>
            ResetTicketsInterval == other.ResetTicketsInterval &&
            RemainingTickets == other.RemainingTickets &&
            NumberOfTicketPurchases == other.NumberOfTicketPurchases &&
            ClearedStageId == other.ClearedStageId;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventDungeonInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ResetTicketsInterval.GetHashCode();
                hashCode = (hashCode * 397) ^ RemainingTickets.GetHashCode();
                hashCode = (hashCode * 397) ^ NumberOfTicketPurchases.GetHashCode();
                hashCode = (hashCode * 397) ^ ClearedStageId.GetHashCode();
                return hashCode;
            }
        }
    }
}

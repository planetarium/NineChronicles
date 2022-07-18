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

        public int RemainingTickets { get; private set; }

        public int ClearedStageId { get; private set; }

        public EventDungeonInfo()
        {
            RemainingTickets = 0;
            ClearedStageId = 0;
        }

        public EventDungeonInfo(Bencodex.Types.List serialized)
        {
            RemainingTickets = serialized[0].ToInteger();
            ClearedStageId = serialized[1].ToInteger();
        }

        public EventDungeonInfo(Bencodex.Types.IValue serialized)
            : this((Bencodex.Types.List)serialized)
        {
        }

        public IValue Serialize() => Bencodex.Types.List.Empty
            .Add(RemainingTickets.Serialize())
            .Add(ClearedStageId.Serialize());

        public void ResetTickets(int tickets)
        {
            if (tickets < 0)
            {
                throw new ArgumentException(
                    $"{nameof(tickets)} must be greater than or equal to 0.");
            }

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

        protected bool Equals(EventDungeonInfo other)
        {
            return RemainingTickets == other.RemainingTickets &&
                   ClearedStageId == other.ClearedStageId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventDungeonInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RemainingTickets, ClearedStageId);
        }
    }
}

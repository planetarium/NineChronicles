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

        private int _remainingTickets;
        private int _clearedStageId;

        public EventDungeonInfo()
        {
            _remainingTickets = 0;
            _clearedStageId = 0;
        }

        public EventDungeonInfo(Bencodex.Types.IValue serialized)
        {
            if (serialized is Bencodex.Types.List list)
            {
                _remainingTickets = list[0].ToInteger();
                _clearedStageId = list[1].ToInteger();
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(serialized)} must be a {typeof(Bencodex.Types.List).FullName}.");
            }
        }

        public IValue Serialize() => Bencodex.Types.List.Empty
            .Add(_remainingTickets.Serialize())
            .Add(_clearedStageId.Serialize());

        public void ResetTickets(int tickets) =>
            _remainingTickets = tickets;

        public bool HasTickets(int tickets) =>
            _remainingTickets >= tickets;

        public bool TryUseTicket(int tickets)
        {
            if (_remainingTickets < tickets)
            {
                return false;
            }

            _remainingTickets -= tickets;
            return true;
        }

        public void ClearStage(int stageId)
        {
            if (_clearedStageId >= stageId)
            {
                return;
            }

            _clearedStageId = stageId;
        }

        public bool IsCleared(int stageId) =>
            _clearedStageId >= stageId;

        protected bool Equals(EventDungeonInfo other)
        {
            return _remainingTickets == other._remainingTickets &&
                   _clearedStageId == other._clearedStageId;
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
            return HashCode.Combine(_remainingTickets, _clearedStageId);
        }
    }
}

using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1027
    /// </summary>
    public class ArenaInformation : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int championshipId, int round) =>
            avatarAddress.Derive($"arena_information_{championshipId}_{round}");

        public const int MaxTicketCount = 8;

        public Address Address;
        public int Win { get; private set; }
        public int Lose { get; private set; }
        public int Ticket { get; private set; }

        public int TicketResetCount { get; private set; }

        public ArenaInformation(Address avatarAddress, int championshipId, int round)
        {
            Address = DeriveAddress(avatarAddress, championshipId, round);
            Ticket = MaxTicketCount;
        }

        public ArenaInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            Win = (Integer)serialized[1];
            Lose = (Integer)serialized[2];
            Ticket = (Integer)serialized[3];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Win)
                .Add(Lose)
                .Add(Ticket);
        }

        public void UseTicket(int value)
        {
            if (Ticket < value)
            {
                throw new NotEnoughTicketException(
                    $"[{nameof(ArenaInformation)}] have({Ticket}) < use({value})");
            }

            Ticket -= value;
        }

        public void UpdateRecord(int win, int lose)
        {
            Win += win;
            Lose += lose;
        }

        public void ResetTicket()
        {
            Ticket = MaxTicketCount;
            TicketResetCount++;
        }
    }
}

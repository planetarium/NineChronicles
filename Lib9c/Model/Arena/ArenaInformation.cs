using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1029
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
        public int PurchasedTicketCount { get; private set; }

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
            TicketResetCount = (Integer)serialized[4];
            PurchasedTicketCount = (Integer)serialized[5];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Win)
                .Add(Lose)
                .Add(Ticket)
                .Add(TicketResetCount)
                .Add(PurchasedTicketCount);
        }

        public void UseTicket(int ticketCount)
        {
            if (Ticket < ticketCount)
            {
                throw new NotEnoughTicketException(
                    $"[{nameof(ArenaInformation)}] have({Ticket}) < use({ticketCount})");
            }

            Ticket -= ticketCount;
        }

        public void BuyTicket(long maxCount)
        {
            if (PurchasedTicketCount >= maxCount)
            {
                throw new ExceedTicketPurchaseLimitException(
                    $"[{nameof(ArenaInformation)}] PurchasedTicketCount({PurchasedTicketCount}) >= MAX({maxCount})");
            }

            PurchasedTicketCount++;
        }

        public void UpdateRecord(int win, int lose)
        {
            Win += win;
            Lose += lose;
        }

        public void ResetTicket(int resetCount)
        {
            Ticket = MaxTicketCount;
            TicketResetCount = resetCount;
        }
    }
}

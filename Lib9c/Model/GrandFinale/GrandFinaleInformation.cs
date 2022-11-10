using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Arena;
using Nekoyume.Model.State;

namespace Nekoyume.Model.GrandFinale
{
    using System.Linq;
    public class GrandFinaleInformation : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int grandFinaleId) =>
            avatarAddress.Derive($"arena_information_{grandFinaleId}");

        public Address Address;
        public int Ticket { get; private set; }
        private Dictionary<Address, bool> BattleRecordDictionary { get; }

        public GrandFinaleInformation(Address avatarAddress, int grandFinaleId, int ticket)
        {
            Address = DeriveAddress(avatarAddress, grandFinaleId);
            Ticket = ticket;
            BattleRecordDictionary = new Dictionary<Address, bool>();
        }

        public GrandFinaleInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            Ticket = (Integer)serialized[1];
            BattleRecordDictionary =
                ((Dictionary)serialized[2]).ToDictionary(pair => pair.Key.ToAddress(),
                    pair => pair.Value.ToBoolean());
        }

        public IValue Serialize()
        {
            var battleRecordDict = new Dictionary(BattleRecordDictionary
                .OrderBy(pair => pair.Key)
                .Select(pair =>
                    new KeyValuePair<IKey, IValue>(
                        (IKey)pair.Key.Serialize(),
                        pair.Value.Serialize()))
            );
            return List.Empty
                .Add(Address.Serialize())
                .Add(Ticket)
                .Add(battleRecordDict);
        }

        public void UseTicket()
        {
            if (Ticket <= 0)
            {
                throw new NotEnoughTicketException(
                    $"[{nameof(GrandFinaleInformation)}] have({Ticket})");
            }

            Ticket -= 1;
        }

        public void UpdateRecord(Address enemyAddress, bool win)
        {
            BattleRecordDictionary.TryAdd(enemyAddress, win);
        }

        public bool TryGetBattleRecord(Address enemyAddress, out bool win) =>
            BattleRecordDictionary.TryGetValue(enemyAddress, out win);
    }
}

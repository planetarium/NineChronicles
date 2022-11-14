using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.GrandFinale
{
    using System.Linq;
    public class GrandFinaleInformation : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int grandFinaleId) =>
            avatarAddress.Derive($"grand_finale_information_{grandFinaleId}");

        public Address Address;
        private Dictionary<Address, bool> BattleRecordDictionary { get; }

        public GrandFinaleInformation(Address avatarAddress, int grandFinaleId)
        {
            Address = DeriveAddress(avatarAddress, grandFinaleId);
            BattleRecordDictionary = new Dictionary<Address, bool>();
        }

        public GrandFinaleInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            BattleRecordDictionary =
                ((Dictionary)serialized[1]).ToDictionary(pair => pair.Key.ToAddress(),
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
                .Add(battleRecordDict);
        }

        public void UpdateRecord(Address enemyAddress, bool win)
        {
            BattleRecordDictionary.Add(enemyAddress, win);
        }

        public bool TryGetBattleRecord(Address enemyAddress, out bool win) =>
            BattleRecordDictionary.TryGetValue(enemyAddress, out win);

        public List<KeyValuePair<Address, bool>> GetBattleRecordList() =>
            BattleRecordDictionary.OrderBy(pair => pair.Key).ToList();
    }
}

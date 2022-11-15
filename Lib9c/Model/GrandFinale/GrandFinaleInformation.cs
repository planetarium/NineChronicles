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
        public const int WinScore = 20;
        public const int LoseScore = 1;
        public const int DefaultScore = 1000;

        public Address Address;
        public int Score { get; private set; }
        private Dictionary<Address, bool> BattleRecordDictionary { get; }

        public GrandFinaleInformation(Address avatarAddress, int grandFinaleId)
        {
            Score = DefaultScore;
            Address = DeriveAddress(avatarAddress, grandFinaleId);
            BattleRecordDictionary = new Dictionary<Address, bool>();
        }

        public GrandFinaleInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            Score = (Integer)serialized[1];
            BattleRecordDictionary = new Dictionary<Address, bool>();
            foreach (var iValue in (List)serialized[2])
            {
                var list = (List)iValue;
                BattleRecordDictionary.Add(list[0].ToAddress(), list[1].ToBoolean());
            }
        }

        public IValue Serialize()
        {
            var battleRecordList = BattleRecordDictionary.OrderBy(kv => kv.Key)
                .Aggregate(List.Empty,
                    (current, kv) =>
                        current.Add(
                            List.Empty
                                .Add(kv.Key.Serialize())
                                .Add(kv.Value.Serialize())
                        )
                );
            return List.Empty
                .Add(Address.Serialize())
                .Add(Score)
                .Add(battleRecordList);
        }

        public void UpdateRecordAndScore(Address enemyAddress, bool win)
        {
            BattleRecordDictionary.Add(enemyAddress, win);
            Score += win ? WinScore : LoseScore;
        }

        public bool TryGetBattleRecord(Address enemyAddress, out bool win) =>
            BattleRecordDictionary.TryGetValue(enemyAddress, out win);

        public List<KeyValuePair<Address, bool>> GetBattleRecordList() =>
            BattleRecordDictionary.OrderBy(pair => pair.Key).ToList();
    }
}

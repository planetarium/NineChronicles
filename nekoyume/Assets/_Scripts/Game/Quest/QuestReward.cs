using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.State;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class QuestReward : IState
    {
        public readonly Dictionary<int, int> ItemMap;

        public QuestReward(Dictionary<int, int> map)
        {
            ItemMap = map;
        }

        public QuestReward(Bencodex.Types.Dictionary serialized)
        {
            ItemMap = serialized.ToDictionary(
                kv => kv.Key.ToInt(),
                kv => kv.Value.ToInt()
            );
        }

        public IValue Serialize()
        {
            return new Bencodex.Types.Dictionary(ItemMap.Select(kv =>
                new KeyValuePair<IKey, IValue>((Text) kv.Key.ToString(), (Text) kv.Value.ToString())));
        } }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class QuestReward : IState
    {
        public readonly Dictionary<int, int> ItemMap;

        public QuestReward(Dictionary<int, int> map)
        {
            ItemMap = map;
        }

        public QuestReward(Dictionary serialized)
        {
            ItemMap = serialized.ToDictionary(
                kv => kv.Key.ToInteger(),
                kv => kv.Value.ToInteger()
            );
        }

        public IValue Serialize()
        {
            return new Dictionary(ItemMap.Select(kv =>
                new KeyValuePair<IKey, IValue>((Text)kv.Key.ToString(), (Text)kv.Value.ToString())));
        }
    }
}

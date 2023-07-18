using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class QuestReward : IState
    {
        public readonly IEnumerable<Tuple<int, int>> ItemMap;

        public QuestReward(Dictionary<int, int> map)
        {
#pragma warning disable LAA1002
            ItemMap = map.Select(kv => new Tuple<int, int>(kv.Key, kv.Value));
        }

        public QuestReward(Dictionary serialized)
        {
            ItemMap = serialized.Select(
                kv => new Tuple<int, int>(kv.Key.ToInteger(), kv.Value.ToInteger())
            );
#pragma warning restore LAA1002
        }

        public IValue Serialize() => new Dictionary(
#pragma warning disable LAA1002
            ItemMap.Select(kv =>
                new KeyValuePair<IKey, IValue>(
                    (Text)kv.Item1.ToString(CultureInfo.InvariantCulture),
                    (Text)kv.Item2.ToString(CultureInfo.InvariantCulture)
                )
            )
#pragma warning restore LAA1002
        );
    }
}

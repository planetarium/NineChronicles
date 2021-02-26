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
        public readonly Dictionary<int, int> ItemMap;

        public QuestReward(Dictionary<int, int> map)
        {
            ItemMap = map
                .ToDictionary(kv => kv.Key, kv => kv.Value
            );
        }

        public QuestReward(Dictionary serialized)
        {
            ItemMap = serialized.ToDictionary(
                kv => kv.Key.ToInteger(),
                kv => kv.Value.ToInteger()
            );
        }

        public IValue Serialize() => new Dictionary(
#pragma warning disable LAA1002
            ItemMap.Select(kv =>
                new KeyValuePair<IKey, IValue>(
                    (Text)kv.Key.ToString(CultureInfo.InvariantCulture),
                    (Text)kv.Value.ToString(CultureInfo.InvariantCulture)
                )
            )
#pragma warning restore LAA1002
        );
    }
}

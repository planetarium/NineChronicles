using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class QuestReward : IState, ISerializable
    {
        private static Codec _codec = new Codec();
        private Dictionary _serialized;
        private Dictionary<int, int> _itemMap;

        public QuestReward(Dictionary<int, int> map)
        {
            _itemMap = map
                .ToDictionary(kv => kv.Key, kv => kv.Value
            );
        }

        public QuestReward(Dictionary serialized)
        {
            _serialized = serialized;
        }

        private QuestReward(SerializationInfo info, StreamingContext context)
            : this((Dictionary)_codec.Decode((byte[])info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public Dictionary<int, int> ItemMap
        {
            get
            {
                if (_itemMap is null)
                {
                    _itemMap = _serialized.ToDictionary(
                        kv => kv.Key.ToInteger(),
                        kv => kv.Value.ToInteger()
                    );
                    _serialized = null;
                }

                return _itemMap;
            }
        }

        public IValue Serialize() => _serialized ?? new Dictionary(
#pragma warning disable LAA1002
            _itemMap.Select(kv =>
#pragma warning restore LAA1002
                new KeyValuePair<IKey, IValue>(
                    (Text)kv.Key.ToString(CultureInfo.InvariantCulture),
                    (Text)kv.Value.ToString(CultureInfo.InvariantCulture)
                )
            )
        );

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", _codec.Encode(Serialize()));
        }
    }
}

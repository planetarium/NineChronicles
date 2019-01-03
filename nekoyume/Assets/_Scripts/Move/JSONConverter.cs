using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Crypto;

namespace Nekoyume.Move
{
    internal class JsonConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(MoveBase).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var name = jo["name"].Value<string>();
            var type = typeof(MoveName).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.IsDefined(typeof(MoveName), false) && MoveName.Extract(t) == name);
            MoveBase move = null;

            if (type != null)
            {
                move = MoveBase.FromPlainValue(jo.ToObject<Dictionary<string, dynamic>>(), type);
            }
            return move;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var move = (MoveBase)value;
            var jo = new JObject();
            foreach (var kv in move.PlainValue)
            {
                jo.Add(kv.Key, JToken.FromObject(kv.Value));
            }
            jo.Add("signature", JToken.FromObject(ByteUtil.Hex(move.Signature)));
            jo.Add("id", JToken.FromObject(ByteUtil.Hex(move.Id)));
            jo.Add("user_public_key", ByteUtil.Hex(move.PublicKey.Format(true)));
            jo.WriteTo(writer);
        }
    }
}

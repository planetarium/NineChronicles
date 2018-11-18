using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Planetarium.Crypto.Extension;

namespace Nekoyume.Move
{
    internal class JsonConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Move).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var name = jo["name"].Value<string>();
            var type = typeof(MoveName).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.IsDefined(typeof(MoveName), false) && MoveName.Extract(t) == name);
            Move move = null;

            if (type != null)
            {
                move = Move.FromPlainValue(jo.ToObject<Dictionary<string, dynamic>>(), type);
            }
            return move;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var move = (Move)value;
            var jo = new JObject();
            foreach (var kv in move.PlainValue)
            {
                jo.Add(kv.Key, JToken.FromObject(kv.Value));
            }
            jo.Add("signature", JToken.FromObject(move.Signature.Hex()));
            jo.Add("id", JToken.FromObject(move.Id.Hex()));
            jo.Add("user_public_key", move.PublicKey.Format(true).Hex());
            jo.WriteTo(writer);
        }
    }
}

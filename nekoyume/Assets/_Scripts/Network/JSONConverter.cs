using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Planetarium.Crypto.Extension;

namespace Nekoyume.Move
{
    internal class JSONConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Move);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var name = jo["name"].Value<string>();
            var type = typeof(MoveName).Assembly
            .GetTypes()
            .Where(t => t.IsDefined(typeof(MoveName), false) && MoveName.Extract(t) == name)
            .FirstOrDefault();
            Move move = null;

            if (type != null)
            {
                move = Move.FromPlainValue(jo.ToObject<Dictionary<string, dynamic>>(), type);
            }

            return move;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

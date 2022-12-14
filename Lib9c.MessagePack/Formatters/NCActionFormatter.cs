using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using MessagePack;
using MessagePack.Formatters;
using Nekoyume.Action;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Formatters
{
    public class NCActionFormatter : IMessagePackFormatter<NCAction?>
    {
        private static readonly IDictionary<string, Type> Types = typeof(ActionBase)
            .Assembly
            .GetTypes()
            .Where(t => t.IsDefined(typeof(ActionTypeAttribute)))
            .ToDictionary(
                t => ActionTypeAttribute.ValueOf(t)
                    ?? throw new InvalidOperationException("Unreachable code."),
                t => t);

        public void Serialize(ref MessagePackWriter writer, PolymorphicAction<ActionBase>? value,
            MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }
            writer.Write(new Codec().Encode(value.PlainValue));
        }

        public PolymorphicAction<ActionBase>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                return null;
            }

            IValue value = new Codec().Decode(bytes.Value.ToArray());
            var plainValue = (Dictionary)value;
            var typeStr = plainValue["type_id"];
            var innerAction = (ActionBase)(Activator.CreateInstance(Types[(Text)typeStr]) ?? throw new InvalidOperationException("Failed to instatiate an action instance."));
            innerAction.LoadPlainValue(plainValue["values"]);
            return new NCAction(innerAction);
        }
    }
}

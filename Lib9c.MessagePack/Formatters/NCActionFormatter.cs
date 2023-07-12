using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using MessagePack;
using MessagePack.Formatters;
using Nekoyume.Action;
using Nekoyume.Action.Loader;

namespace Lib9c.Formatters
{
    public class NCActionFormatter : IMessagePackFormatter<ActionBase?>
    {
        private readonly IActionLoader _actionLoader;

        public NCActionFormatter()
        {
            _actionLoader = new NCActionLoader();
        }

        public void Serialize(ref MessagePackWriter writer, ActionBase? value,
            MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }
            writer.Write(new Codec().Encode(value.PlainValue));
        }

        public ActionBase? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                return null;
            }

            // NOTE: Passing index 0 might not be suitable.
            IValue value = new Codec().Decode(bytes.Value.ToArray());
            return (ActionBase)_actionLoader.LoadAction(0, value);
        }
    }
}

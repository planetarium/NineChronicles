using System;
using System.Buffers;
using Bencodex;
using Bencodex.Types;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class BencodexFormatter<T> : IMessagePackFormatter<T> where T: IValue
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            writer.Write(new Codec().Encode(value));
        }

        T IMessagePackFormatter<T>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                throw new NullReferenceException($"ReadBytes from serialized {typeof(T).Name} is null.");
            }

            return (T)new Codec().Decode(bytes.Value.ToArray());
        }
    }
}

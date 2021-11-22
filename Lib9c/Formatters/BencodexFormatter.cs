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

            return (T)new Codec().Decode(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
        }
    }
}

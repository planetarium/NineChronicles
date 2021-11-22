using System;
using System.Buffers;
using Libplanet.Crypto;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class PublicKeyFormatter : IMessagePackFormatter<PublicKey>
    {
        public void Serialize(ref MessagePackWriter writer, PublicKey value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Format(true));
        }

        public PublicKey Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            return new PublicKey(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
        }
    }
}

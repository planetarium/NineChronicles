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

            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                throw new NullReferenceException($"ReadBytes from serialized {nameof(PublicKey)} is null.");
            }

            return new PublicKey(bytes.Value.ToArray());
        }
    }
}

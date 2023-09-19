using System;
using System.Buffers;
using System.Security.Cryptography;
using Libplanet.Common;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class HashDigestFormatter : IMessagePackFormatter<HashDigest<SHA256>>
    {
        public void Serialize(
            ref MessagePackWriter writer,
            HashDigest<SHA256> value,
            MessagePackSerializerOptions options)
        {
            writer.Write(value.ToByteArray());
        }

        public HashDigest<SHA256> Deserialize(
            ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            var bytes = reader.ReadBytes()?.ToArray();
            if (bytes is null)
            {
                throw new InvalidOperationException();
            }

            return HashDigest<SHA256>.DeriveFrom(bytes);
        }
    }
}

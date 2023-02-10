using System;
using System.Buffers;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    // FIXME: This class must be removed and replaced with other way for serialization.
    // https://github.com/dotnet/designs/blob/main/accepted/2020/better-obsoletion/binaryformatter-obsoletion.md
    public class ExceptionFormatter<T> : IMessagePackFormatter<T?> where T : Exception
    {
        public void Serialize(ref MessagePackWriter writer, T? value,
            MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
#pragma warning disable SYSLIB0011
                formatter.Serialize(stream, value);
#pragma warning restore SYSLIB0011
                var bytes = stream.ToArray();
                writer.Write(bytes);
            }
        }

        public T? Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatter = new BinaryFormatter();
            byte[] bytes = reader.ReadBytes()?.ToArray()
                ?? throw new MessagePackSerializationException();
            using (var stream = new MemoryStream(bytes))
            {
#pragma warning disable SYSLIB0011
                return (T)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011
            }
        }
    }
}

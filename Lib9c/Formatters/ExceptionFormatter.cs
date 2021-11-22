using System;
using System.Buffers;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class ExceptionFormatter<T> : IMessagePackFormatter<T> where T : Exception
    {
        public void Serialize(ref MessagePackWriter writer, T value,
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
                formatter.Serialize(stream, value);
                var bytes = stream.ToArray();
                writer.Write(bytes);
            }
        }

        T IMessagePackFormatter<T>.Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatter = new BinaryFormatter();
            byte[] bytes = reader.ReadBytes()?.ToArray();
            using (var stream = new MemoryStream(bytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}

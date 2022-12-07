using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Nekoyume
{
    public static class ByteSerializer
    {
        public static byte[] Serialize<T>(T items)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, items);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}

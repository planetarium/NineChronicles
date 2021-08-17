namespace Lib9c.Tests.Model
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Xunit;

    public class ExceptionTest
    {
        public static void AssertException(params Exception[] exceptions)
        {
            foreach (var exception in exceptions)
            {
                var formatter = new BinaryFormatter();
                using var ms = new MemoryStream();
                formatter.Serialize(ms, exception);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (Exception)Convert.ChangeType(formatter.Deserialize(ms), exception.GetType());
                Assert.Equal(exception.Message, deserialized.Message);
            }
        }
    }
}

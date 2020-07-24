namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Action;
    using Xunit;

    public class ExceptionTest
    {
        [Fact]
        public void AlreadyReceivedExceptionSerializable()
        {
            var exc = new AlreadyReceivedException("for testing");

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (AlreadyReceivedException)formatter.Deserialize(ms);
                Assert.Equal("for testing", deserialized.Message);
            }
        }

        [Fact]
        public void ArenaNotEndedExceptionSerializable()
        {
            var exc = new ArenaNotEndedException("for testing");

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (ArenaNotEndedException)formatter.Deserialize(ms);
                Assert.Equal("for testing", deserialized.Message);
            }
        }

        [Fact]
        public void FailedLoadStateExceptionSerializable()
        {
            var exc = new FailedLoadStateException("for testing");

            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (FailedLoadStateException)formatter.Deserialize(ms);
                Assert.Equal("for testing", deserialized.Message);
            }
        }
    }
}

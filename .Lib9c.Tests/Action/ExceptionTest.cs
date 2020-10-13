namespace Lib9c.Tests.Action
{
    using System;
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
            AssertException<AlreadyReceivedException>(exc);
        }

        [Fact]
        public void ArenaNotEndedExceptionSerializable()
        {
            var exc = new ArenaNotEndedException("for testing");
            AssertException<ArenaNotEndedException>(exc);
        }

        [Fact]
        public void AvatarIndexAlreadyUsedExceptionSerializable()
        {
            var exc = new AvatarIndexAlreadyUsedException("for testing");
            AssertException<AvatarIndexAlreadyUsedException>(exc);
        }

        [Fact]
        public void FailedLoadStateExceptionSerializable()
        {
            var exc = new FailedLoadStateException("for testing");
            AssertException<FailedLoadStateException>(exc);
        }

        [Fact]
        public void InvalidNamePatternExceptionSerializable()
        {
            var exc = new InvalidNamePatternException("for testing");
            AssertException<InvalidNamePatternException>(exc);
        }

        [Fact]
        public void CombinationSlotUnlockExceptionSerializable()
        {
            var exc = new CombinationSlotUnlockException("for testing");
            AssertException<CombinationSlotUnlockException>(exc);
        }

        [Fact]
        public void NotEnoughMaterialExceptionSerializable()
        {
            var exc = new NotEnoughMaterialException("for testing");
            AssertException<NotEnoughMaterialException>(exc);
        }

        [Fact]
        public void InvalidPriceExceptionSerializable()
        {
            var exc = new InvalidPriceException("for testing");
            AssertException<InvalidPriceException>(exc);
        }

        [Fact]
        public void ItemDoesNotExistException()
        {
            var exc = new ItemDoesNotExistException("for testing");
            AssertException<ItemDoesNotExistException>(exc);
        }

        private static void AssertException<T>(Exception exc)
            where T : Exception
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (T)formatter.Deserialize(ms);
                Assert.Equal(exc.Message, deserialized.Message);
            }
        }
    }
}

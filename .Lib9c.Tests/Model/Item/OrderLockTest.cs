namespace Lib9c.Tests.Model.Item
{
    using System;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Xunit;

    public class OrderLockTest
    {
        [Fact]
        public void Serialize()
        {
            var orderLock = new OrderLock(Guid.NewGuid());
            var deserialized = new OrderLock((List)orderLock.Serialize());
            Assert.Equal(LockType.Order, deserialized.Type);
            Assert.Equal(orderLock.OrderId, deserialized.OrderId);
        }
    }
}

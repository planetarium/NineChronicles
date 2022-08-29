namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Xunit;

    public class OrderDigestListStateTest
    {
        private readonly OrderDigest _orderDigest;

        public OrderDigestListStateTest()
        {
            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var tradableId = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");
            _orderDigest = new OrderDigest(
                default,
                default,
                1,
                orderId,
                tradableId,
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                Currency.Legacy("NCG", 2, null) * 1,
#pragma warning restore CS0618
                2,
                3,
                4,
                1
            );
        }

        [Fact]
        public void Add()
        {
            var address = OrderDigestListState.DeriveAddress(default);
            var orderDigestList = new OrderDigestListState(address);
            orderDigestList.Add(_orderDigest);

            Assert.Single(orderDigestList.OrderDigestList);
            OrderDigest orderDigest = orderDigestList.OrderDigestList.First();
            Assert.Equal(_orderDigest, orderDigest);

            Assert.Throws<DuplicateOrderIdException>(() => orderDigestList.Add(_orderDigest));
        }

        [Fact]
        public void Serialize()
        {
            var address = OrderDigestListState.DeriveAddress(default);
            var orderDigestList = new OrderDigestListState(address);
            orderDigestList.Add(_orderDigest);

            Dictionary serialized = (Dictionary)orderDigestList.Serialize();
            Assert.Equal(orderDigestList, new OrderDigestListState(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var address = OrderDigestListState.DeriveAddress(default);
            var orderDigestList = new OrderDigestListState(address);
            orderDigestList.Add(_orderDigest);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, orderDigestList);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderDigestListState)formatter.Deserialize(ms);

            Assert.Equal(orderDigestList.Serialize(), deserialized.Serialize());
        }
    }
}

namespace Lib9c.Tests.Action
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Xunit;

    public class PurchaseInfoTest
    {
        [Fact]
        public void Serialize()
        {
            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var tradableId = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");
            var price = new FungibleAssetValue(new Currency("NCG", 2, minter: null), 100, 0);
            var purchaseInfo = new PurchaseInfo(
                orderId,
                tradableId,
                Addresses.Shop,
                Addresses.Admin,
                ItemSubType.Food,
                price
            );

            Dictionary serialized = (Dictionary)purchaseInfo.Serialize();

            Assert.Equal(purchaseInfo, new PurchaseInfo(serialized));

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, purchaseInfo);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (PurchaseInfo)formatter.Deserialize(ms);
            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}

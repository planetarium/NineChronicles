namespace Lib9c.Tests.Model.Mail
{
    using System;
    using Bencodex.Types;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class MaterialCraftMailTest
    {
        [Fact]
        public void Serialize()
        {
            var mail = new MaterialCraftMail(
                1,
                Guid.NewGuid(),
                2,
                3,
                10020001);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = (MaterialCraftMail)Mail.Deserialize(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(3, deserialized.ItemCount);
            Assert.Equal(10020001, deserialized.ItemId);
        }
    }
}

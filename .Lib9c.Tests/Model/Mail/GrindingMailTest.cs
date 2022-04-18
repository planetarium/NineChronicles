namespace Lib9c.Tests.Model.Mail
{
    using System;
    using Bencodex.Types;
    using Libplanet.Assets;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class GrindingMailTest
    {
        private readonly Currency _currency = new Currency("CRYSTAL", 2, minters: null);

        [Fact]
        public void Serialize()
        {
            var mail = new GrindingMail(1, Guid.NewGuid(), 2, 3, _currency * 1000);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = (GrindingMail)Mail.Deserialize(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(3, deserialized.ItemCount);
            Assert.Equal(_currency * 1000, deserialized.Asset);
        }
    }
}

namespace Lib9c.Tests.Model.Mail
{
    using System;
    using Bencodex.Types;
    using Libplanet.Assets;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class GrindingMailTest
    {
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private readonly Currency _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

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

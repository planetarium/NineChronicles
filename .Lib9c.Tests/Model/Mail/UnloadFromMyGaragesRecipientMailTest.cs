namespace Lib9c.Tests.Model.Mail
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Tests.Action.Garages;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class UnloadFromMyGaragesRecipientMailTest
    {
        public static IEnumerable<object[]> Get_Sample_PlainValue() =>
            UnloadFromMyGaragesTest.Get_Sample_PlainValue().Select(objects =>
                new[]
                {
                    // objects[0], This test doesn't need to test recipientAvatarAddr.
                    objects[1],
                    objects[2],
                    objects[3],
                });

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            const long blockIndex = 0L;
            var guid = Guid.NewGuid();
            var mail = new UnloadFromMyGaragesRecipientMail(
                blockIndex,
                guid,
                blockIndex,
                fungibleAssetValues,
                fungibleIdAndCounts,
                memo);
            var mailValue = mail.Serialize();
            var de = new UnloadFromMyGaragesRecipientMail((Dictionary)mailValue);
            Assert.True(mail.FungibleAssetValues?.SequenceEqual(de.FungibleAssetValues!) ??
                        de.FungibleAssetValues is null);
            Assert.True(mail.FungibleIdAndCounts?.SequenceEqual(de.FungibleIdAndCounts!) ??
                        de.FungibleIdAndCounts is null);
            Assert.Equal(mail.Memo, de.Memo);
            var mailValue2 = de.Serialize();
            Assert.Equal(mailValue, mailValue2);
        }
    }
}

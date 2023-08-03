#nullable enable
namespace Lib9c.Tests.Model.Coupons
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Common;
    using Nekoyume.Model.Coupons;
    using Xunit;

    public class CouponTest
    {
        [Fact]
        public void Constructors()
        {
            var emptyCoupon = new Coupon(
                Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                default(RewardSet)
            );
            Assert.Equal(
                emptyCoupon,
                new Coupon(
                    Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                    Enumerable.Empty<(int, uint)>()
                )
            );
            Assert.Equal(
                emptyCoupon,
                new Coupon(
                    Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                    new (int, uint)[0]
                )
            );

            var coupon = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (3, 4U))
            );
            Assert.Equal(
                coupon,
                new Coupon(
                    Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                    new List<(int, uint)> { (1, 2U), (3, 4U) }
                )
            );
            Assert.Equal(
                coupon,
                new Coupon(
                    Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                    (1, 2U),
                    (3, 4U)
                )
            );
        }

        [Fact]
        public void Equality()
        {
            var emptyCoupon = new Coupon(
                Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                default(RewardSet)
            );
            var coupon = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (3, 4U))
            );
            var coupon2 = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (3, 5U))
            );
            var coupon3 = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (2, 4U))
            );
            var coupon4 = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U))
            );
            var coupon5 = new Coupon(
                Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                new RewardSet((1, 2U), (3, 4U))
            );
            Assert.False(coupon.Equals(emptyCoupon));
            Assert.True(coupon.Equals(coupon));
            Assert.False(coupon.Equals(coupon2));
            Assert.False(coupon.Equals(coupon3));
            Assert.False(coupon.Equals(coupon4));
            Assert.False(coupon.Equals(coupon5));
            Assert.False(emptyCoupon.Equals(coupon5));
            Assert.False(coupon.Equals((object)emptyCoupon));
            Assert.True(coupon.Equals((object)coupon));
            Assert.False(coupon.Equals((object)coupon2));
            Assert.False(coupon.Equals((object)coupon3));
            Assert.False(coupon.Equals((object)coupon4));
            Assert.False(coupon.Equals((object)coupon5));
            Assert.False(emptyCoupon.Equals((object)coupon5));
            Assert.False(coupon.Equals((object?)null));
            Assert.False(emptyCoupon.Equals((object?)null));
            Assert.NotEqual(coupon.GetHashCode(), emptyCoupon.GetHashCode());
            Assert.Equal(coupon.GetHashCode(), coupon.GetHashCode());
            Assert.NotEqual(coupon.GetHashCode(), coupon2.GetHashCode());
            Assert.NotEqual(coupon.GetHashCode(), coupon3.GetHashCode());
            Assert.NotEqual(coupon.GetHashCode(), coupon4.GetHashCode());
            Assert.NotEqual(coupon.GetHashCode(), coupon5.GetHashCode());
            Assert.NotEqual(emptyCoupon.GetHashCode(), coupon5.GetHashCode());
        }

        [Fact]
        public void Serialize()
        {
            var emptyCoupon = new Coupon(
                Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                default(RewardSet)
            );
            var emptySerialized = emptyCoupon.Serialize();
            Assert.Equal(
                Bencodex.Types.Dictionary.Empty
                    .Add("id", ByteUtil.ParseHex("829e898b1819724d83c883407f8d6dc0"))
                    .Add("rewards", Bencodex.Types.Dictionary.Empty),
                emptySerialized
            );

            var coupon = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (3, 4U))
            );
            var serialized = coupon.Serialize();
            Assert.Equal(
                Bencodex.Types.Dictionary.Empty
                    .Add("id", ByteUtil.ParseHex("d1d5094a02d7d545aa6f44e19bdab42d"))
                    .Add("rewards", Bencodex.Types.Dictionary.Empty.Add("1", 2).Add("3", 4)),
                serialized
            );
        }

        [Fact]
        public void Deserialize()
        {
            var deserializedEmptyCoupon = new Coupon(
                Bencodex.Types.Dictionary.Empty
                    .Add("id", ByteUtil.ParseHex("829e898b1819724d83c883407f8d6dc0"))
                    .Add("rewards", Bencodex.Types.Dictionary.Empty)
            );
            var expectedEmptyCoupon = new Coupon(
                Guid.Parse("8b899e82-1918-4d72-83c8-83407f8d6dc0"),
                default(RewardSet)
            );
            Assert.Equal(expectedEmptyCoupon, deserializedEmptyCoupon);

            var deserializedCoupon = new Coupon(
                Bencodex.Types.Dictionary.Empty
                    .Add("id", ByteUtil.ParseHex("d1d5094a02d7d545aa6f44e19bdab42d"))
                    .Add("rewards", Bencodex.Types.Dictionary.Empty.Add("1", 2).Add("3", 4))
            );
            var expectedCoupon = new Coupon(
                Guid.Parse("4a09d5d1-d702-45d5-aa6f-44e19bdab42d"),
                new RewardSet((1, 2U), (3, 4U))
            );
            Assert.Equal(expectedCoupon, deserializedCoupon);
        }
    }
}

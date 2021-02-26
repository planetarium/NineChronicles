namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Xunit;

    public class CreditsStateTest
    {
        [Fact]
        public void Serialize()
        {
            var credits = new CreditsState(
                new[]
                {
                    "John Smith",
                    "홍길동",
                    "山田太郎",
                }
            );

            Dictionary serialized = (Dictionary)credits.Serialize();
            var deserialized = new CreditsState(serialized);

            Assert.Equal(credits.Names, deserialized.Names);
        }
    }
}

namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Nekoyume.Action;
    using Xunit;

    public class RuneSlotInfoTest
    {
        [Fact]
        public void Serialize()
        {
            var info = new RuneSlotInfo(1, 1);
            var serialized = info.Serialize();
            var deserialized = new RuneSlotInfo((List)serialized);

            Assert.Equal(1, deserialized.RuneId);
            Assert.Equal(1, deserialized.SlotIndex);
        }
    }
}

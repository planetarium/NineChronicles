namespace Lib9c.Tests.Model.Rune
{
    using Bencodex.Types;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Xunit;

    public class RuneSlotTest
    {
        [Fact]
        public void Serialize()
        {
            var slot = new RuneSlot(0, RuneSlotType.Default, RuneType.Stat, true);
            var serialized = (List)slot.Serialize();
            var deserialized = new RuneSlot(serialized);

            Assert.Equal(slot.RuneId, deserialized.RuneId);
            Assert.Equal(slot.RuneSlotType, deserialized.RuneSlotType);
            Assert.Equal(slot.RuneType, deserialized.RuneType);
            Assert.Equal(slot.Index, deserialized.Index);
            Assert.Equal(slot.IsLock, deserialized.IsLock);
        }
    }
}

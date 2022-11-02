namespace Lib9c.Tests.Model
{
    using Bencodex.Types;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Xunit;

    public class RuneSlotStateTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new RuneSlotState(BattleType.Adventure);
            var serialized = (List)state.Serialize();
            var deserialized = new RuneSlotState(serialized);

            Assert.Equal(state.Serialize(), deserialized.Serialize());
        }
    }
}

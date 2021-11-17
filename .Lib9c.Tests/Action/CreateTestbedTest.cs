namespace Lib9c.Tests.Action
{
    using Lib9c.DevExtensions.Action;
    using Lib9c.Tests.TestHelper;
    using Xunit;

    public class CreateTestbedTest
    {
        [Fact]
        public void Execute()
        {
            var state = BlockChainHelper.MakeInitState();
            var action = new CreateTestbed();
            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
            });
        }
    }
}

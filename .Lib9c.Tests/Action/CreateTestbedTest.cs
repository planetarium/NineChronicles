namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Lib9c.Tests.TestHelper;
    using Xunit;

    public class CreateTestbedTest
    {
        [Fact]
        public void Execute()
        {
            var result = BlockChainHelper.MakeInitialState();
            var testbed = result.GetTestbed();
            Assert.Equal(testbed.Orders.Count(), testbed.result.ItemInfos.Count);
        }
    }
}

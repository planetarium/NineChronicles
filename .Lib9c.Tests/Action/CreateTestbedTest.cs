namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Lib9c.DevExtensions;
    using Lib9c.DevExtensions.Action;
    using Lib9c.DevExtensions.Model;
    using Lib9c.Tests.TestHelper;
    using Nekoyume.Action;
    using Xunit;

    public class CreateTestbedTest
    {
        [Fact]
        public void Execute()
        {
            var result = BlockChainHelper.MakeInitState();
            var testbed = result.GetTestbed();
            var nextState = result.GetState();
            var data = TestbedHelper.LoadData<TestbedSell>("TestbedSell");

            Assert.Equal(testbed.Orders.Count(), testbed.result.ItemInfos.Count);

            for (var i = 0; i < testbed.Orders.Count; i++)
            {
                Assert.Equal(data.Items[i].ItemSubType, testbed.Orders[i].ItemSubType);
            }

            var purchaseInfos = new List<PurchaseInfo>();
            foreach (var order in testbed.Orders)
            {
                var purchaseInfo = new PurchaseInfo(
                    order.OrderId,
                    order.TradableId,
                    order.SellerAgentAddress,
                    order.SellerAvatarAddress,
                    order.ItemSubType,
                    order.Price
                );
                purchaseInfos.Add(purchaseInfo);
            }

            var buyAction = new Buy
            {
                buyerAvatarAddress = result.GetAvatarState().address,
                purchaseInfos = purchaseInfos,
            };

            nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 100,
                PreviousStates = nextState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = result.GetAgentState().address,
            });
        }
    }
}

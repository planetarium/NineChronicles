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
            var action = new CreateTestbed();
            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = result.GetState(),
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            path = path.Replace(
                ".Lib9c.Tests\\bin\\Debug\\netcoreapp3.1",
                "Lib9c.DevExtensions\\Data\\TestbedSell.json");
            path = path.Replace("file:\\", string.Empty);
            var data = TestbedHelper.LoadJsonFile<TestbedSell>(path);

            Assert.Equal(action.Orders.Count(), action.result.ItemInfos.Count);

            for (var i = 0; i < action.Orders.Count; i++)
            {
                Assert.Equal(data.Items[i].ItemSubType, action.Orders[i].ItemSubType);
            }

            var purchaseInfos = new List<PurchaseInfo>();
            foreach (var order in action.Orders)
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

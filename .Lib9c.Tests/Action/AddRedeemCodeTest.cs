namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class AddRedeemCodeTest
    {
        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var initStates = MockState.Empty
                .SetState(AdminState.Address, adminState.Serialize());
            var state = new Account(initStates);
            var action = new AddRedeemCode
            {
                redeemCsv = "New Value",
            };

            PolicyExpiredException exc1 = Assert.Throws<PolicyExpiredException>(() =>
            {
                action.Execute(
                    new ActionContext
                    {
                        BlockIndex = 101,
                        PreviousState = state,
                        Signer = adminAddress,
                    }
                );
            });
            Assert.Equal(101, exc1.BlockIndex);

            PermissionDeniedException exc2 = Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(
                    new ActionContext
                    {
                        BlockIndex = 5,
                        PreviousState = state,
                        Signer = new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"),
                    }
                );
            });
            Assert.Equal(new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"), exc2.Signer);
        }

        [Fact]
        public void Execute()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);

            var csv = TableSheetsImporter.ImportSheets()[nameof(RedeemCodeListSheet)];

            var action = new AddRedeemCode
            {
                redeemCsv = csv,
            };

            var nextState = action.Execute(new ActionContext
            {
                Signer = adminAddress,
                BlockIndex = 0,
                PreviousState = new Account(MockState.Empty)
                    .SetState(Addresses.Admin, adminState.Serialize())
                    .SetState(Addresses.RedeemCode, new RedeemCodeState(new RedeemCodeListSheet()).Serialize()),
            });

            var sheet = new RedeemCodeListSheet();
            sheet.Set(csv);
            var expectedMap = new RedeemCodeState(sheet).Map;
            var redeemState = nextState.GetRedeemCodeState();
            foreach (var (key, reward) in expectedMap)
            {
                Assert.Equal(reward.RewardId, redeemState.Map[key].RewardId);
            }
        }

        [Fact]
        public void ExecuteThrowSheetRowValidateException()
        {
            var csv = TableSheetsImporter.ImportSheets()[nameof(RedeemCodeListSheet)];
            var sheet = new RedeemCodeListSheet();
            sheet.Set(csv);

            var state = new Account(MockState.Empty)
                    .SetState(Addresses.RedeemCode, new RedeemCodeState(sheet).Serialize());

            var action = new AddRedeemCode
            {
                redeemCsv = csv,
            };

            Assert.Throws<SheetRowValidateException>(() => action.Execute(new ActionContext
                {
                    BlockIndex = 0,
                    PreviousState = state,
                })
            );
        }
    }
}

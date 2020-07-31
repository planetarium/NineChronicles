namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class PatchTableSheetsTest
    {
        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var initStates = ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, adminState.Serialize())
                .Add(TableSheetsState.Address, new TableSheetsState(new Dictionary<string, string>()
                {
                    ["TestTable"] = "Initial",
                }).Serialize());
            var state =
                new State(initStates, ImmutableDictionary<(Address, Currency), BigInteger>.Empty);
            var action = new PatchTableSheet()
            {
                TableName = "TestTable",
                TableCsv = "New Value",
            };

            PolicyExpiredException exc1 = Assert.Throws<PolicyExpiredException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 101,
                        PreviousStates = state,
                        Signer = adminAddress,
                    }
                );
            });
            Assert.Equal(101, exc1.BlockIndex);

            PermissionDeniedException exc2 = Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 5,
                        PreviousStates = state,
                        Signer = new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"),
                    }
                );
            });
            Assert.Equal(new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"), exc2.Signer);
        }
    }
}

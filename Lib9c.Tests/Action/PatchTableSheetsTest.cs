using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model.State;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Lib9c.Tests.Action
{
    public class PatchTableSheetsTest
    {
        class State : IAccountStateDelta
        {
            private readonly ImmutableDictionary<Address, IValue> _state;
            public IImmutableSet<Address> UpdatedAddresses =>
                _state.Keys.ToImmutableHashSet();

            public State(ImmutableDictionary<Address, IValue> state)
            {
                _state = state;
            }

            public IValue GetState(Address address)
            {
                return _state[address];
            }

            public IAccountStateDelta SetState(Address address, IValue state)
            {
                return new State(_state.SetItem(address, state));
            }
        }
        public class ActionContext : IActionContext
        {
            public Address Signer { get; set; }

            public Address Miner { get; set; }

            public long BlockIndex { get; set; }

            public bool Rehearsal { get; set; }

            public IAccountStateDelta PreviousStates { get; set; }

            public IRandom Random => throw new System.NotImplementedException();
        }

        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, adminState.Serialize())
                .Add(TableSheetsState.Address, new TableSheetsState(new Dictionary<string, string>()
                {
                    ["TestTable"] = "Initial",
                }).Serialize())
            );
            var action = new PatchTableSheet()
            {
                TableName = "TestTable",
                TableCsv = "New Value"
            };

            Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 101,
                        PreviousStates = state,
                        Signer = adminAddress
                    }
                );
            });

            Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 5,
                        PreviousStates = state,
                        Signer = new Address("019101FEec7ed4f918D396827E1277DEda1e20D4")
                    }
                );
            });
        }
    }
}

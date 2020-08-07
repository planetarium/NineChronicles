namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class OpenChestTest
    {
        [Fact]
        public void Rehearsal()
        {
            var action = new OpenChest();
            var address = default(Address);
            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = address,
                Rehearsal = true,
                BlockIndex = 1,
            });

            Assert.Equal(
                ImmutableHashSet.Create(
                    address, GoldCurrencyState.Address
                ),
                nextState.UpdatedAddresses
            );
        }
    }
}

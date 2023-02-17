using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.BlockChain;

namespace Lib9c.Miner.Tests;

public class CustomActionsDeserializableValidatorTest
{
    [Fact]
    public void Validate()
    {
        var validator = new CustomActionsDeserializableValidator(new MockActionTypeLoader(), 10);
        Assert.False(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")),
        }));
        Assert.False(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")
                    .Add("values", Dictionary.Empty.Add("a", ImmutableArray<byte>.Empty))),
        }));
        Assert.True(validator.Validate(new MockTransaction
        {
            CustomActions =
                ImmutableArray<IValue>.Empty.Add(Dictionary.Empty.Add("type_id", "daily_reward")
                    .Add("values", Dictionary.Empty.Add("a", new Address().ByteArray))),
        }));
    }

    private class DailyReward : IAction
    {
        private Address AvatarAddress { get; set; }

        public IValue PlainValue => Dictionary.Empty.Add("a", AvatarAddress.ByteArray);

        public void LoadPlainValue(IValue plainValue)
        {
            AvatarAddress = new Address(((Binary)((Dictionary)plainValue)["a"]).ByteArray);
        }

        public IAccountStateDelta Execute(IActionContext context)
        {
            return context.PreviousStates;
        }
    }

    private class MockTransaction : ITransaction
    {
        public long Nonce { get; init; }
        public Address Signer { get; init; }
        public IImmutableSet<Address> UpdatedAddresses { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public PublicKey PublicKey { get; init; }
        public BlockHash? GenesisHash { get; init; }
        public TxId Id { get; init; }
        public byte[] Signature { get; init; }
        public Dictionary? SystemAction { get; init; }
        public IImmutableList<IValue>? CustomActions { get; init; }
    }

    private class MockActionTypeLoader : IActionTypeLoader
    {
        public IDictionary<string, Type> Load(IActionTypeLoaderContext context)
        {
            return new Dictionary<string, Type>
            {
                ["daily_reward"] = typeof(DailyReward),
            };
        }

        public IEnumerable<Type> LoadAllActionTypes(IActionTypeLoaderContext context)
        {
            return Load(context).Values;
        }
    }
}

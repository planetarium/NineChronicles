using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.BlockChain;

namespace Lib9c.Proposer.Tests;

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
        public TxActionList Actions =>
            new(SystemAction is { } sa ? new List(sa) : new List(CustomActions!));
        public TxId Id { get; init; }
        public byte[] Signature { get; init; }
        public IValue? SystemAction { get; init; }
        public IImmutableList<IValue>? CustomActions { get; init; }
        public bool Equals(ITxInvoice? other)
        {
            return UpdatedAddresses.Equals(other.UpdatedAddresses) &&
                   Timestamp.Equals(other.Timestamp) &&
                   Nullable.Equals(GenesisHash, other.GenesisHash) &&
                   Actions.Equals(other.Actions);
        }

        public bool Equals(ITxSigningMetadata? other)
        {
            return Nonce == other.Nonce &&
                   Signer.Equals(other.Signer) &&
                   PublicKey.Equals(other.PublicKey);
        }

        public bool Equals(IUnsignedTx? other)
        {
            return ((ITxSigningMetadata)this).Equals(other) &&
                   ((ITxInvoice)this).Equals(other);
        }
    }

    private class MockActionTypeLoader : IActionTypeLoader
    {
        public IDictionary<IValue, Type> Load(IActionTypeLoaderContext context)
        {
            return new Dictionary<IValue, Type>
            {
                [(Text)"daily_reward"] = typeof(DailyReward),
            };
        }

        public IEnumerable<Type> LoadAllActionTypes(IActionTypeLoaderContext context)
        {
            return Load(context).Values;
        }
    }
}

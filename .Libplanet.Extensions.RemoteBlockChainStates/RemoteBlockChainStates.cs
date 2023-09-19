using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates
{
    public class RemoteBlockChainStates : IBlockChainStates
    {
        private readonly Uri _explorerEndpoint;

        public RemoteBlockChainStates(Uri explorerEndpoint)
        {
            _explorerEndpoint = explorerEndpoint;
        }

        public IValue? GetState(Address address, BlockHash? offset) =>
            GetStates(new[] { address }, offset).First();

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses, BlockHash? offset)
        {
            return new RemoteBlockState(_explorerEndpoint, offset).GetStates(addresses);
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency, BlockHash? offset)
        {
            return new RemoteBlockState(_explorerEndpoint, offset).GetBalance(address, currency);
        }

        public FungibleAssetValue GetTotalSupply(Currency currency, BlockHash? offset)
        {
            return new RemoteBlockState(_explorerEndpoint, offset).GetTotalSupply(currency);
        }

        public ValidatorSet GetValidatorSet(BlockHash? offset)
        {
            return new RemoteBlockState(_explorerEndpoint, offset).GetValidatorSet();
        }

        public IAccountState GetAccountState(BlockHash? offset) =>
            throw new NotImplementedException();

        public IAccountState GetBlockState(BlockHash? offset)
        {
            return new RemoteBlockState(_explorerEndpoint, offset);
        }

        public IAccountState GetAccountState(HashDigest<SHA256>? hash) =>
            throw new NotImplementedException();
    }
}

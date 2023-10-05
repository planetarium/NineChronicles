using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Action.State;
using Libplanet.Types.Tx;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class ActionContext : IActionContext
{
    public ActionContext(
        BlockHash? genesisHash,
        Address signer,
        TxId? txId,
        Address miner,
        long blockIndex,
        int blockProtocolVersion,
        bool rehearsal,
        AccountStateDelta previousState,
        int randomSeed,
        HashDigest<SHA256>? previousStateRootHash,
        bool blockAction)
    {
        GenesisHash = genesisHash;
        Signer = signer;
        TxId = txId;
        Miner = miner;
        BlockIndex = blockIndex;
        BlockProtocolVersion = blockProtocolVersion;
        Rehearsal = rehearsal;
        PreviousState = previousState;
        RandomSeed = randomSeed;
        PreviousStateRootHash = previousStateRootHash;
        BlockAction = blockAction;
    }

    public BlockHash? GenesisHash { get; }
    public Address Signer { get; init; }
    public TxId? TxId { get; }
    public Address Miner { get; init; }
    public long BlockIndex { get; init; }
    public int BlockProtocolVersion { get; init; }
    public bool Rehearsal { get; init; }
    public AccountStateDelta PreviousState { get; init; }
    IAccount IActionContext.PreviousState => PreviousState;
    public int RandomSeed { get; init; }
    public HashDigest<SHA256>? PreviousStateRootHash { get; init; }
    public bool BlockAction { get; init; }

    public IRandom GetRandom() => new Random(RandomSeed);

    public void UseGas(long gas)
    {
        throw new NotImplementedException();
    }

    public long GasUsed() => 0;

    public long GasLimit() => 0;
}

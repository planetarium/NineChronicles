using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Boolean = Bencodex.Types.Boolean;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public static class ActionContextMarshaller
{
    private static readonly Codec Codec = new Codec();

    public static byte[] Serialize(this ICommittedActionContext actionContext)
    {
        return Codec.Encode(Marshal(actionContext));
    }

    public static Dictionary Marshal(this ICommittedActionContext actionContext)
    {
        var dictionary = Bencodex.Types.Dictionary.Empty
            .Add("signer", actionContext.Signer.ToHex())
            .Add("miner", actionContext.Miner.ToHex())
            .Add("block_index", actionContext.BlockIndex)
            .Add("block_protocol_version", actionContext.BlockProtocolVersion)
            .Add("rehearsal", actionContext.Rehearsal)
            .Add("previous_states", actionContext.PreviousState.ByteArray)
            .Add("random_seed", actionContext.RandomSeed)
            .Add("block_action", actionContext.BlockAction);

        if (actionContext.TxId is { } txId)
        {
            dictionary = dictionary.Add("tx_id", txId.ByteArray);
        }

        return dictionary;
    }

    public static ICommittedActionContext Unmarshal(Dictionary dictionary)
    {
        return new CommittedActionContext(
            signer: new Address(((Text)dictionary["signer"]).Value),
            txId: dictionary.TryGetValue((Text)"tx_id", out IValue txIdValue) &&
                  txIdValue is Binary txIdBinaryValue
                ? new TxId(txIdBinaryValue.ByteArray)
                : null,
            miner: new Address(((Text)dictionary["miner"]).Value),
            blockIndex: (Integer)dictionary["block_index"],
            blockProtocolVersion: (Integer)dictionary["block_protocol_version"],
            rehearsal: (Boolean)dictionary["rehearsal"],
            previousState: new HashDigest<SHA256>(((Binary)dictionary["previous_states"]).ByteArray),
            randomSeed: (Integer)dictionary["random_seed"],
            blockAction: (Boolean)dictionary["block_action"]
        );
    }

    public static ICommittedActionContext Deserialize(byte[] serialized)
    {
        var decoded = Codec.Decode(serialized);
        if (!(decoded is Dictionary dictionary))
        {
            throw new ArgumentException($"Expected 'Dictionary' but {decoded.GetType().Name}", nameof(serialized));
        }

        return Unmarshal(dictionary);
    }
}

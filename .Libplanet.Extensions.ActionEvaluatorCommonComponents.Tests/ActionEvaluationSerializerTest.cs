using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents.Tests;

public class ActionEvaluationSerializerTest
{
    [Fact]
    public void Serialization()
    {
        IKeyValueStore keyValueStore = new MemoryKeyValueStore();
        IStateStore stateStore = new TrieStateStore(keyValueStore);
        var addresses = Enumerable.Repeat(0, 4).Select(_ => new PrivateKey().ToAddress()).ToImmutableList();
        AccountStateDelta outputStates = (AccountStateDelta)new AccountStateDelta()
            .SetState(addresses[0], Null.Value)
            .SetState(addresses[1], (Text)"foo")
            .SetState(addresses[2], new List((Text)"bar"));
        var previousTrie = stateStore.GetStateRoot(null);
        var nextTrie = previousTrie;
        foreach (var kv in outputStates.Delta.ToRawDelta())
        {
            nextTrie = nextTrie.Set(kv.Key, kv.Value);
        }
        nextTrie = stateStore.Commit(nextTrie);

        var committed = new CommittedActionEvaluation(
            action: Null.Value,
            inputContext: new CommittedActionContext(
                signer: addresses[0],
                txId: null,
                miner: addresses[1],
                blockIndex: 0,
                blockProtocolVersion: 0,
                rehearsal: false,
                previousState: previousTrie.Hash,
                randomSeed: 123,
                blockAction: true),
            outputState: nextTrie.Hash,
            exception: new UnexpectedlyTerminatedActionException(
                "",
                null,
                null,
                null,
                null,
                new NullAction(),
                new Exception()));
        var serialized = ActionEvaluationMarshaller.Serialize(committed);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.Equal(Null.Value, deserialized.Action);
        Assert.Equal(123, deserialized.InputContext.RandomSeed);
        Assert.Equal(0, deserialized.InputContext.BlockIndex);
        Assert.Equal(0, deserialized.InputContext.BlockProtocolVersion);
        Assert.Equal(addresses[0], deserialized.InputContext.Signer);
        Assert.Equal(addresses[1], deserialized.InputContext.Miner);
        Assert.Equal(previousTrie.Hash, deserialized.InputContext.PreviousState);
        Assert.Equal(nextTrie.Hash, deserialized.OutputState);
    }
}

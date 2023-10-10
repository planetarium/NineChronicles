using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.StateService.Shared;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Extensions.ActionEvaluatorCommonComponents;
using Libplanet.Common;

namespace Libplanet.Extensions.RemoteActionEvaluator;

public class RemoteActionEvaluator : IActionEvaluator
{
    private readonly Uri _endpoint;

    public RemoteActionEvaluator(Uri endpoint)
    {
        _endpoint = endpoint;
    }

    public IActionLoader ActionLoader => throw new NotSupportedException();

    public IReadOnlyList<ICommittedActionEvaluation> Evaluate(
        IPreEvaluationBlock block, HashDigest<SHA256>? baseStateRootHash)
    {
        using var httpClient = new HttpClient();
        var response = httpClient.PostAsJsonAsync(_endpoint, new RemoteEvaluationRequest
        {
            PreEvaluationBlock = PreEvaluationBlockMarshaller.Serialize(block),
            BaseStateRootHash = baseStateRootHash is null
                ? new byte[]{}
                : baseStateRootHash.Value.ToByteArray(),
        }).Result;
        var evaluationResponse = response.Content.ReadFromJsonAsync<RemoteEvaluationResponse>().Result;

        var actionEvaluations = evaluationResponse.Evaluations.Select(ActionEvaluationMarshaller.Deserialize)
            .ToImmutableList();

        return actionEvaluations;
    }

    [Pure]
    private static IReadOnlyList<IValue?> NullAccountStateGetter(
        IReadOnlyList<Address> addresses
    ) =>
        new IValue?[addresses.Count];

    [Pure]
    private static FungibleAssetValue NullAccountBalanceGetter(
        Address address,
        Currency currency
    ) =>
        currency * 0;

    [Pure]
    private static FungibleAssetValue NullTotalSupplyGetter(Currency currency)
    {
        if (!currency.TotalSupplyTrackable)
        {
            throw WithDefaultMessage(currency);
        }

        return currency * 0;
    }

    [Pure]
    private static ValidatorSet NullValidatorSetGetter()
    {
        return new ValidatorSet();
    }

    private static TotalSupplyNotTrackableException WithDefaultMessage(Currency currency)
    {
        var msg =
            $"The total supply value of the currency {currency} is not trackable because it"
            + " is a legacy untracked currency which might have been established before"
            + " the introduction of total supply tracking support.";
        return new TotalSupplyNotTrackableException(msg, currency);
    }
}

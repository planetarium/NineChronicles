using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Lib9c.StateService.Shared;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Types.Blocks;
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
}

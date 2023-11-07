using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Lib9c.StateService.Shared;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Extensions.ActionEvaluatorCommonComponents;
using Libplanet.Store;
using Microsoft.AspNetCore.Mvc;
using Nekoyume.Action;
using Nekoyume.Action.Loader;

namespace Lib9c.StateService.Controllers;

[ApiController]
[Route("/evaluation")]
public class RemoteEvaluationController : ControllerBase
{
    private readonly IActionEvaluator _actionEvaluator;
    private readonly ILogger<RemoteEvaluationController> _logger;
    private readonly Codec _codec;

    public RemoteEvaluationController(
        IStateStore stateStore,
        ILogger<RemoteEvaluationController> logger,
        Codec codec)
    {
        _actionEvaluator = new ActionEvaluator(
            _ => new RewardGold(),
            stateStore,
            new NCActionLoader());
        _logger = logger;
        _codec = codec;
    }

    [HttpPost]
    public ActionResult<RemoteEvaluationResponse> GetEvaluation([FromBody] RemoteEvaluationRequest request)
    {
        var decoded = _codec.Decode(request.PreEvaluationBlock);
        if (decoded is not Dictionary dictionary)
        {
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var decodedStateRootHash = _codec.Decode(request.BaseStateRootHash);
        if (decodedStateRootHash is not Binary binary)
        {
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var preEvaluationBlock = PreEvaluationBlockMarshaller.Unmarshal(dictionary);
        var baseStateRootHash = new HashDigest<SHA256>(binary);

        return Ok(new RemoteEvaluationResponse
        {
            Evaluations = _actionEvaluator
                .Evaluate(preEvaluationBlock, baseStateRootHash)
                .Select(ActionEvaluationMarshaller.Serialize)
                .ToArray(),
        });
    }
}

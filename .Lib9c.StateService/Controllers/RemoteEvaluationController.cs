using Bencodex;
using Bencodex.Types;
using Lib9c.StateService.Shared;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Extensions.ActionEvaluatorCommonComponents;
using Microsoft.AspNetCore.Mvc;
using Nekoyume.Action;
using Nekoyume.Action.Loader;

namespace Lib9c.StateService.Controllers;

[ApiController]
[Route("/evaluation")]
public class RemoteEvaluationController : ControllerBase
{
    private readonly IBlockChainStates _blockChainStates;
    private readonly ILogger<RemoteEvaluationController> _logger;
    private readonly Codec _codec;

    public RemoteEvaluationController(
        IBlockChainStates blockChainStates,
        ILogger<RemoteEvaluationController> logger,
        Codec codec)
    {
        _blockChainStates = blockChainStates;
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

        var preEvaluationBlock = PreEvaluationBlockMarshaller.Unmarshal(dictionary);
        var actionEvaluator =
            new ActionEvaluator(
                context => new RewardGold(),
                _blockChainStates,
                new NCActionLoader());
        return Ok(new RemoteEvaluationResponse
        {
            Evaluations = actionEvaluator.Evaluate(preEvaluationBlock).Select(ActionEvaluationMarshaller.Serialize)
                .ToArray(),
        });
    }
}

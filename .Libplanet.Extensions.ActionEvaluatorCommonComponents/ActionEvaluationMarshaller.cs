using Bencodex;
using Bencodex.Types;
using Libplanet.Action;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public static class ActionEvaluationMarshaller
{
    private static readonly Codec Codec = new Codec();

    public static byte[] Serialize(this IActionEvaluation actionEvaluation)
    {
        return Codec.Encode(Marshal(actionEvaluation));
    }

    public static IEnumerable<Dictionary> Marshal(this IEnumerable<IActionEvaluation> actionEvaluations)
    {
        var actionEvaluationsArray = actionEvaluations.ToArray();
        var outputStates = AccountStateDeltaMarshaller.Marshal(actionEvaluationsArray.Select(aev => aev.OutputState));
        var previousStates = AccountStateDeltaMarshaller.Marshal(actionEvaluationsArray.Select(aev => aev.InputContext.PreviousState));
        foreach (var actionEvaluation in actionEvaluationsArray)
        {
            yield return Dictionary.Empty
                .Add("action", actionEvaluation.Action)
                .Add("output_states", AccountStateDeltaMarshaller.Marshal(actionEvaluation.OutputState))
                .Add("input_context", ActionContextMarshaller.Marshal(actionEvaluation.InputContext))
                .Add("exception", actionEvaluation.Exception?.GetType().FullName is { } typeName ? (Text)typeName : Null.Value);
        }
    }

    public static Dictionary Marshal(this IActionEvaluation actionEvaluation)
    {
        return Dictionary.Empty
            .Add("action", actionEvaluation.Action)
            .Add("output_states", AccountStateDeltaMarshaller.Marshal(actionEvaluation.OutputState))
            .Add("input_context", ActionContextMarshaller.Marshal(actionEvaluation.InputContext))
            .Add("exception", actionEvaluation.Exception?.GetType().FullName is { } typeName ? (Text)typeName : Null.Value);
    }

    public static ActionEvaluation Unmarshal(IValue value)
    {
        if (value is not Dictionary dictionary)
        {
            throw new ArgumentException(nameof(value));
        }

        return new ActionEvaluation(
            dictionary["action"],
            ActionContextMarshaller.Unmarshal((Dictionary)dictionary["input_context"]),
            AccountStateDeltaMarshaller.Unmarshal(dictionary["output_states"]),
            dictionary["exception"] is Text typeName ? new Exception(typeName) : null
        );
    }

    public static ActionEvaluation Deserialize(byte[] serialized)
    {
        var decoded = Codec.Decode(serialized);
        return Unmarshal(decoded);
    }
}

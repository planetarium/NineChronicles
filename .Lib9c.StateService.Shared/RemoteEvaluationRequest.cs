namespace Lib9c.StateService.Shared;

public class RemoteEvaluationRequest
{
    public byte[] PreEvaluationBlock { get; set; }

    public byte[] BaseStateRootHash { get; set; }
}

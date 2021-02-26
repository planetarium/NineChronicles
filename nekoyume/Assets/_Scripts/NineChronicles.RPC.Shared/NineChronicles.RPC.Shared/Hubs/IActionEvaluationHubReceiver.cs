namespace Nekoyume.Shared.Hubs
{
    public interface IActionEvaluationHubReceiver
    {
        void OnRender(byte[] evaluation);
        
        void OnUnrender(byte[] evaluation);

        void OnRenderBlock(byte[] oldTip, byte[] newTip);

        void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint);

        void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint);

        void OnException(int code, string message);

        void OnPreloadStart();

        void OnPreloadEnd();
    }
}

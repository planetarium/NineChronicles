namespace Nekoyume.Shared.Hubs
{
    public interface IActionEvaluationHubReceiver
    {
        void OnRender(byte[] evaluation);

        void OnTipChanged(long index);
    }
}

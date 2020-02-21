using MagicOnion;
using System.Threading.Tasks;

namespace Nekoyume.Shared.Hubs
{
    public interface IActionEvaluationHub : IStreamingHub<IActionEvaluationHub, IActionEvaluationHubReceiver>
    {
        Task JoinAsync();

        Task LeaveAsync();

        Task BroadcastAsync(byte[] encoded);

        Task UpdateTipAsync(long index);
    }
}

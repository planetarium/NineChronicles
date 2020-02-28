using System.Threading.Tasks;
using MagicOnion.Server.Hubs;
using Nekoyume.Shared.Hubs;

namespace NineChronicles.Standalone
{
    public class ActionEvaluationHub : StreamingHubBase<IActionEvaluationHub, IActionEvaluationHubReceiver>, IActionEvaluationHub
    {
        private IGroup group;

        public async Task JoinAsync()
        {
            group = await Group.AddAsync(string.Empty);
        }

        public async Task LeaveAsync()
        {
            await group.RemoveAsync(Context);
        }

        public async Task BroadcastAsync(byte[] outputStates)
        {
            Broadcast(group).OnRender(outputStates);
            await Task.CompletedTask;
        }

        public async Task UpdateTipAsync(long index)
        {
            Broadcast(group).OnTipChanged(index);
            await Task.CompletedTask;
        }
    }
}

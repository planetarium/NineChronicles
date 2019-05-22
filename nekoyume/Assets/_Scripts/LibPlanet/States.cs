using Nekoyume.State;
using UniRx;

namespace Nekoyume
{
    public class States
    {
        public static readonly ReactiveProperty<AgentState> Agent = new ReactiveProperty<AgentState>();
        public static readonly ReactiveProperty<AvatarState> Avatar = new ReactiveProperty<AvatarState>();
        public static readonly ReactiveProperty<ShopState> Shop = new ReactiveProperty<ShopState>();
    }
}

using Nekoyume.Model;
using Nekoyume.State;
using UniRx;

namespace Nekoyume
{
    /// <summary>
    /// 각 주소 고유의 상태들을 모아서 데이터 지역성을 확보한다.
    /// </summary>
    public class States
    {
        public static readonly ReactiveProperty<AgentState> Agent = new ReactiveProperty<AgentState>();
        public static readonly ReactiveProperty<AvatarState> Avatar = new ReactiveProperty<AvatarState>();
        public static readonly ReactiveProperty<ShopState> Shop = new ReactiveProperty<ShopState>();

        static States()
        {
            Agent.Subscribe(AgentSubscribe);
            Avatar.Subscribe(AvatarSubscribe);
            Shop.Subscribe(ShopSubscribe);
        }

        private static void AgentSubscribe(AgentState agentState)
        {
            ReactiveAgentState.Initialize(agentState);
        }
        
        private static void AvatarSubscribe(AvatarState avatarState)
        {
            ReactiveAvatarState.Initialize(avatarState);
        }
        
        private static void ShopSubscribe(ShopState shopState)
        {
            ReactiveShopState.Initialize(shopState);
        }
    }
}

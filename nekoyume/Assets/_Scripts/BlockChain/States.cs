using Nekoyume.Model;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 각 주소 고유의 상태들을 모아서 데이터 지역성을 확보한다.
    /// </summary>
    public class States
    {
        private static class Singleton
        {
            internal static readonly States Value = new States();

            static Singleton()
            {
            }
        }

        public static readonly States Instance = Singleton.Value;
        
        public readonly ReactiveProperty<AgentState> agentState = new ReactiveProperty<AgentState>();
        public readonly ReactiveDictionary<int, AvatarState> avatarStates = new ReactiveDictionary<int, AvatarState>();
        public readonly ReactiveProperty<AvatarState> currentAvatarState = new ReactiveProperty<AvatarState>();
        public readonly ReactiveProperty<RankingState> rankingState = new ReactiveProperty<RankingState>();
        public readonly ReactiveProperty<ShopState> shopState = new ReactiveProperty<ShopState>();

        private States()
        {
            agentState.Subscribe(AgentSubscribe);
            currentAvatarState.Subscribe(CurrentAvatarSubscribe);
            rankingState.Subscribe(RankingSubscribe);
            shopState.Subscribe(ShopSubscribe);
        }

        private static void AgentSubscribe(AgentState agentState)
        {
            ReactiveAgentState.Initialize(agentState);
        }
        
        private static void CurrentAvatarSubscribe(AvatarState avatarState)
        {
            ReactiveCurrentAvatarState.Initialize(avatarState);
        }
        
        private static void RankingSubscribe(RankingState rankingState)
        {
            ReactiveRankingState.Initialize(rankingState);
        }
        
        private static void ShopSubscribe(ShopState shopState)
        {
            ReactiveShopState.Initialize(shopState);
        }
    }
}

using System;
using UniRx;

namespace Nekoyume.State
{
    /// <summary>
    /// 각 주소 고유의 상태들을 모아서 데이터 지역성을 확보한다.
    /// </summary>
    public class States : IDisposable
    {
        public static States Instance => Game.Game.instance.States;

        public readonly ReactiveProperty<AgentState> AgentState = new ReactiveProperty<AgentState>();
        public readonly ReactiveDictionary<int, AvatarState> AvatarStates = new ReactiveDictionary<int, AvatarState>();
        public readonly ReactiveProperty<int> CurrentAvatarKey = new ReactiveProperty<int>(-1);
        public readonly ReactiveProperty<AvatarState> CurrentAvatarState = new ReactiveProperty<AvatarState>();
        public readonly ReactiveProperty<RankingState> RankingState = new ReactiveProperty<RankingState>();
        public readonly ReactiveProperty<ShopState> ShopState = new ReactiveProperty<ShopState>();

        public States()
        {
            AgentState.Subscribe(SubscribeAgent);
            AvatarStates.ObserveAdd().Subscribe(SubscribeAvatarStatesAdd);
            AvatarStates.ObserveRemove().Subscribe(SubscribeAvatarStatesRemove);
            AvatarStates.ObserveReplace().Subscribe(SubscribeAvatarStatesReplace);
            AvatarStates.ObserveReset().Subscribe(SubscribeAvatarStatesReset);
            CurrentAvatarKey.Subscribe(SubscribeCurrentAvatarKey);
            CurrentAvatarState.Subscribe(SubscribeCurrentAvatar);
            RankingState.Subscribe(SubscribeRanking);
            ShopState.Subscribe(SubscribeShop);
        }
        
        public void Dispose()
        {
            AgentState?.Dispose();
            AvatarStates?.Dispose();
            CurrentAvatarKey?.Dispose();
            CurrentAvatarState?.Dispose();
            RankingState?.Dispose();
            ShopState?.Dispose();
        }

        #region Subscribe

        private static void SubscribeAgent(AgentState value)
        {
            ReactiveAgentState.Initialize(value);
        }

        private static void SubscribeCurrentAvatar(AvatarState value)
        {
            ReactiveCurrentAvatarState.Initialize(value);
        }

        private static void SubscribeRanking(RankingState value)
        {
            ReactiveRankingState.Initialize(value);
        }

        private static void SubscribeShop(ShopState value)
        {
            ReactiveShopState.Initialize(value);
        }

        private void SubscribeAvatarStatesAdd(DictionaryAddEvent<int, AvatarState> e)
        {
            if (e.Key == CurrentAvatarKey.Value)
            {
                CurrentAvatarState.Value = AvatarStates[e.Key];
            }
        }

        private void SubscribeAvatarStatesRemove(DictionaryRemoveEvent<int, AvatarState> e)
        {
            if (e.Key == CurrentAvatarKey.Value)
            {
                CurrentAvatarKey.Value = -1;
            }
        }

        private void SubscribeAvatarStatesReplace(DictionaryReplaceEvent<int, AvatarState> e)
        {
            if (e.Key == CurrentAvatarKey.Value)
            {
                SubscribeCurrentAvatarKey(e.Key);
            }
        }

        private void SubscribeAvatarStatesReset(Unit unit)
        {
            CurrentAvatarKey.Value = -1;
        }

        private void SubscribeCurrentAvatarKey(int value)
        {
            if (!AvatarStates.ContainsKey(value))
            {
                CurrentAvatarState.Value = null;
                return;
            }

            CurrentAvatarState.Value = AvatarStates[value];
        }

        #endregion
    }
}

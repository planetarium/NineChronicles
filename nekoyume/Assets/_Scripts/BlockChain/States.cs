using System;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.State;
using UniRx;
using UnityEngine;

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
        public readonly ReactiveProperty<int> currentAvatarKey = new ReactiveProperty<int>(-1);
        public readonly ReactiveProperty<AvatarState> currentAvatarState = new ReactiveProperty<AvatarState>();
        public readonly ReactiveProperty<RankingState> rankingState = new ReactiveProperty<RankingState>();
        public readonly ReactiveProperty<ShopState> shopState = new ReactiveProperty<ShopState>();

        public const string CurrentAvatarKey = "agent_{0}_avatar_{1}";

        private States()
        {
            agentState.Subscribe(SubscribeAgent);
            avatarStates.ObserveAdd().Subscribe(SubscribeAvatarStatesAdd);
            avatarStates.ObserveRemove().Subscribe(SubscribeAvatarStatesRemove);
            avatarStates.ObserveReplace().Subscribe(SubscribeAvatarStatesReplace);
            avatarStates.ObserveReset().Subscribe(SubscribeAvatarStatesReset);
            currentAvatarKey.Subscribe(SubscribeCurrentAvatarKey);
            currentAvatarState.Subscribe(SubscribeCurrentAvatar);
            rankingState.Subscribe(SubscribeRanking);
            shopState.Subscribe(SubscribeShop);
        }

        private void SubscribeAgent(AgentState value)
        {
            ReactiveAgentState.Initialize(value);
        }

        private void SubscribeAvatarStatesAdd(DictionaryAddEvent<int, AvatarState> e)
        {
            if (e.Key == currentAvatarKey.Value)
            {
                currentAvatarState.Value = avatarStates[e.Key];
            }
        }
        
        private void SubscribeAvatarStatesRemove(DictionaryRemoveEvent<int, AvatarState> e)
        {
            if (e.Key == currentAvatarKey.Value)
            {
                currentAvatarKey.Value = -1;
            }
        }
        
        private void SubscribeAvatarStatesReplace(DictionaryReplaceEvent<int, AvatarState> e)
        {
            if (e.Key == currentAvatarKey.Value)
            {
                SubscribeCurrentAvatarKey(currentAvatarKey.Value);
            }
        }
        
        private void SubscribeAvatarStatesReset(Unit unit)
        {
            currentAvatarKey.Value = -1;
        }

        private void SubscribeCurrentAvatarKey(int value)
        {
            if (!avatarStates.ContainsKey(value))
            {
                currentAvatarState.Value = null;
                return;
            }

            currentAvatarState.Value = avatarStates[value];
        }
        
        private void SubscribeCurrentAvatar(AvatarState value)
        {
            ReactiveCurrentAvatarState.Initialize(value);
        }
        
        private void SubscribeRanking(RankingState value)
        {
            ReactiveRankingState.Initialize(value);
        }
        
        private void SubscribeShop(ShopState value)
        {
            ReactiveShopState.Initialize(value);
        }

        private static bool WantsToQuit()
        {
            SaveLocalAvatarState();
            return true;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void RunOnStart()
        {
            Application.wantsToQuit += WantsToQuit;
        }

        public static void SaveLocalAvatarState()
        {
            if (!(Instance.currentAvatarState?.Value is null))
            {
                var avatarState = Instance.currentAvatarState.Value;
                Debug.LogFormat("Save local avatarState. agentAddress: {0} address: {1} BlockIndex: {2}",
                    avatarState.agentAddress, avatarState.address, avatarState.BlockIndex);

                var key = string.Format(CurrentAvatarKey, avatarState.agentAddress,
                    avatarState.address);
                var serializedAvatar =
                    Convert.ToBase64String(ByteSerializer.Serialize(avatarState));
                PlayerPrefs.SetString(key, serializedAvatar);
            }

        }

    }
}

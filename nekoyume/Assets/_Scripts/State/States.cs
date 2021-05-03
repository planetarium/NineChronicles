using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nekoyume.State
{
    /// <summary>
    /// 클라이언트가 참조할 상태를 포함한다.
    /// 체인의 상태를 Setter를 통해서 받은 후, 로컬의 상태로 필터링해서 사용한다.
    /// </summary>
    public class States
    {
        public static States Instance => Game.Game.instance.States;

        public readonly Dictionary<Address, RankingMapState> RankingMapStates = new Dictionary<Address, RankingMapState>();

        public WeeklyArenaState WeeklyArenaState { get; private set; }

        public AgentState AgentState { get; private set; }

        public GoldBalanceState GoldBalanceState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new Dictionary<int, AvatarState>();
        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public GameConfigState GameConfigState { get; private set; }

        public readonly Dictionary<Address, CombinationSlotState> CombinationSlotStates =
            new Dictionary<Address, CombinationSlotState>();

        private HashSet<Model.State.RankingInfo> rankingInfoSet = null;

        public List<AbilityRankingModel> AbilityRankingInfos = null;

        public List<StageRankingModel> StageRankingInfos = null;

        public List<StageRankingModel> MimisbrunnrRankingInfos = null;

        public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

        public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, StageRankingModel> AgentMimisbrunnrRankingInfos = new Dictionary<int, StageRankingModel>();

        public States()
        {
            DeselectAvatar();
        }

        #region Setter

        /// <summary>
        /// 랭킹 상태를 할당한다.
        /// </summary>
        /// <param name="state"></param>
        public void SetRankingMapStates(RankingMapState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetRankingMapStates)}] {nameof(state)} is null.");
                return;
            }

            RankingMapStates[state.address] = state;
            RankingMapStatesSubject.OnNext(RankingMapStates);
        }

        public void SetWeeklyArenaState(WeeklyArenaState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetWeeklyArenaState)}] {nameof(state)} is null.");
                return;
            }

            LocalLayer.Instance.InitializeWeeklyArena(state);
            WeeklyArenaState = LocalLayer.Instance.Modify(state);
            WeeklyArenaStateSubject.OnNext(WeeklyArenaState);
        }

        /// <summary>
        /// 에이전트 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// 최초로 할당하거나 기존과 다른 주소의 에이전트를 할당하면, 모든 아바타 상태를 새롭게 할당된다.
        /// </summary>
        /// <param name="state"></param>
        public void SetAgentState(AgentState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetAgentState)}] {nameof(state)} is null.");
                return;
            }

            var getAllOfAvatarStates =
                AgentState is null ||
                !AgentState.address.Equals(state.address);

            LocalLayer.Instance.InitializeAgentAndAvatars(state);
            AgentState = LocalLayer.Instance.Modify(state);

            if (!getAllOfAvatarStates)
            {
                return;
            }

            foreach (var pair in AgentState.avatarAddresses)
            {
                AddOrReplaceAvatarState(pair.Value, pair.Key);
            }
        }

        public void SetGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetGoldBalanceState)}] {nameof(goldBalanceState)} is null.");
                return;
            }

            GoldBalanceState = LocalLayer.Instance.Modify(goldBalanceState);
            AgentStateSubject.Gold.OnNext(GoldBalanceState.Gold);
        }

        public AvatarState AddOrReplaceAvatarState(Address avatarAddress, int index, bool initializeReactiveState = true)
        {
            var avatarState =
                new AvatarState((Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(avatarAddress));
            return AddOrReplaceAvatarState(avatarState, index, initializeReactiveState);
        }

        /// <summary>
        /// 아바타 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        public AvatarState AddOrReplaceAvatarState(AvatarState state, int index, bool initializeReactiveState = true)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(AddOrReplaceAvatarState)}] {nameof(state)} is null.");
                return null;
            }

            if (AgentState is null || !AgentState.avatarAddresses.ContainsValue(state.address))
                throw new Exception(
                    $"`AgentState` is null or not found avatar's address({state.address}) in `AgentState`");

            state = LocalLayer.Instance.Modify(state);

            if (_avatarStates.ContainsKey(index))
            {
                _avatarStates[index] = state;
            }
            else
            {
                _avatarStates.Add(index, state);
            }

            return index == CurrentAvatarKey
                ? SelectAvatar(index, initializeReactiveState)
                : state;
        }

        /// <summary>
        /// 인자로 받은 인덱스의 아바타 상태를 제거한다.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void RemoveAvatarState(int index)
        {
            if (!_avatarStates.ContainsKey(index))
                throw new KeyNotFoundException($"{nameof(index)}({index})");

            _avatarStates.Remove(index);

            if (index == CurrentAvatarKey)
            {
                DeselectAvatar();
            }
        }

        /// <summary>
        /// 인자로 받은 인덱스의 아바타 상태를 선택한다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public AvatarState SelectAvatar(int index, bool initializeReactiveState = true)
        {
            if (!_avatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            var isNew = CurrentAvatarKey != index;

            CurrentAvatarKey = index;
            var avatarState = _avatarStates[CurrentAvatarKey];
            LocalLayer.Instance.InitializeCurrentAvatarState(avatarState);
            UpdateCurrentAvatarState(avatarState, initializeReactiveState);

            if (isNew)
            {
                // NOTE: 새로운 아바타를 처음 선택할 때에는 모든 워크샵 슬롯을 업데이트 합니다.
                SetCombinationSlotStates(avatarState);
            }

            if (Game.Game.instance.Agent is RPCAgent agent)
            {
                agent.UpdateSubscribeAddresses();
            }

            return CurrentAvatarState;
        }

        /// <summary>
        /// 아바타 상태 선택을 해지한다.
        /// </summary>
        public void DeselectAvatar()
        {
            CurrentAvatarKey = -1;
            LocalLayer.Instance?.InitializeCurrentAvatarState(null);
            UpdateCurrentAvatarState(null);
        }

        private void SetCombinationSlotStates(AvatarState avatarState)
        {
            if (avatarState is null)
            {
                LocalLayer.Instance.InitializeCombinationSlotsByCurrentAvatarState(null);

                return;
            }

            LocalLayer.Instance.InitializeCombinationSlotsByCurrentAvatarState(avatarState);
            for (var i = 0; i < avatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = avatarState.address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                var slotState = new CombinationSlotState(
                    (Dictionary) Game.Game.instance.Agent.GetState(slotAddress));
                SetCombinationSlotState(slotState);
            }
        }

        public void SetCombinationSlotState(CombinationSlotState state)
        {
            state = LocalLayer.Instance.Modify(state);
            CombinationSlotStates[state.address] = state;

            CombinationSlotStateSubject.OnNext(state);
        }

        public void SetGameConfigState(GameConfigState state)
        {
            GameConfigState = state;
            GameConfigStateSubject.OnNext(state);
        }

        #endregion

        /// <summary>
        /// `CurrentAvatarKey`에 따라서 `CurrentAvatarState`를 업데이트 한다.
        /// </summary>
        private void UpdateCurrentAvatarState(AvatarState state, bool initializeReactiveState = true)
        {
            CurrentAvatarState = state;

            if (!initializeReactiveState)
                return;

            ReactiveAvatarState.Initialize(CurrentAvatarState);
        }

        public void UpdateRanking()
        {
            var rankingMapStates = RankingMapStates;
            rankingInfoSet = new HashSet<RankingInfo>();
            foreach (var pair in rankingMapStates)
            {
                var rankingInfo = pair.Value.GetRankingInfos(null);
                rankingInfoSet.UnionWith(rankingInfo);
            }

            Debug.LogWarning($"total user count : {rankingInfoSet.Count()}");

            var sw = new Stopwatch();
            sw.Start();

            LoadAbilityRankingInfos();
            //LoadStageRankingInfo();
            //LoadMimisbrunnrRankingInfo();

            sw.Stop();
            UnityEngine.Debug.LogWarning($"total elapsed : {sw.Elapsed}");
        }

        private void LoadAbilityRankingInfos()
        {
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;

            var rankOffset = 1;
            AbilityRankingInfos = rankingInfoSet
                .OrderByDescending(i => i.Level)
                .Take(100)
                .Select(rankingInfo =>
                {
                    var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);
                    var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);

                    return new AbilityRankingModel()
                    {
                        AvatarState = avatarState,
                        Cp = cp,
                    };
                })
                .ToList()
                .OrderByDescending(i => i.Cp)
                .ThenByDescending(i => i.AvatarState.level)
                .ToList();
            AbilityRankingInfos.ForEach(i => i.Rank = rankOffset++);

            foreach (var pair in _avatarStates)
            {
                var avatarState = pair.Value;
                var avatarAddress = avatarState.address;
                var index = AbilityRankingInfos.FindIndex(i => i.AvatarState.address.Equals(avatarAddress));
                if (index >= 0)
                {
                    var info = AbilityRankingInfos[index];

                    AgentAbilityRankingInfos[pair.Key] =
                        new AbilityRankingModel()
                        {
                            Rank = index + 1,
                            AvatarState = avatarState,
                            Cp = info.Cp,
                        };
                }
                else
                {
                    var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);

                    AgentAbilityRankingInfos[pair.Key] =
                        new AbilityRankingModel()
                        {
                            AvatarState = avatarState,
                            Cp = cp,
                        };
                }
            }
        }

        private void LoadStageRankingInfo()
        {
            var orderedAvatarStates = rankingInfoSet
                .Select(rankingInfo =>
                {
                    var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                    return avatarState;
                })
                .ToList()
                .OrderByDescending(x => x.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0)
                .ToList();

            foreach (var pair in _avatarStates)
            {
                var avatarState = pair.Value;
                var avatarAddress = avatarState.address;
                var index = orderedAvatarStates.FindIndex(i => i.address.Equals(avatarAddress));
                if (index >= 0)
                {
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                    AgentStageRankingInfos[pair.Key] = 
                        new StageRankingModel()
                        {
                            Rank = index + 1,
                            AvatarState = avatarState,
                            Stage = stageProgress,
                        };
                }
            }

            StageRankingInfos = orderedAvatarStates
                .Take(RankPanel.RankingBoardDisplayCount)
                .Select(avatarState =>
                {
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                    return new StageRankingModel()
                    {
                        AvatarState = avatarState,
                        Stage = stageProgress,
                    };
                }).ToList();
        }

        private void LoadMimisbrunnrRankingInfo()
        {
            var orderedAvatarStates = rankingInfoSet
                .Select(rankingInfo =>
                {
                    var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                    return avatarState;
                })
                .ToList()
                .OrderByDescending(x => x.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0)
                .ToList();

            foreach (var pair in _avatarStates)
            {
                var avatarState = pair.Value;
                var avatarAddress = avatarState.address;
                var index = orderedAvatarStates.FindIndex(i => i.address.Equals(avatarAddress));
                if (index >= 0)
                {
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                    AgentMimisbrunnrRankingInfos[pair.Key] =
                        new StageRankingModel()
                        {
                            Rank = index + 1,
                            AvatarState = avatarState,
                            Stage = stageProgress > 0 ?
                                stageProgress - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                        };
                }
            }

            MimisbrunnrRankingInfos = orderedAvatarStates
                .Take(RankPanel.RankingBoardDisplayCount)
                .Select(avatarState =>
                {
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                    return new StageRankingModel()
                    {
                        AvatarState = avatarState,
                        Stage = stageProgress > 0 ?
                            stageProgress - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                    };
                }).ToList();
        }
    }
}

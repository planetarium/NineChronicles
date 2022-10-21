using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Debug = UnityEngine.Debug;
using static Lib9c.SerializeKeys;
using StateExtensions = Nekoyume.Model.State.StateExtensions;
using Libplanet.Assets;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.UI;

namespace Nekoyume.State
{
    /// <summary>
    /// 클라이언트가 참조할 상태를 포함한다.
    /// 체인의 상태를 Setter를 통해서 받은 후, 로컬의 상태로 필터링해서 사용한다.
    /// </summary>
    public class States
    {
        public static States Instance => Game.Game.instance.States;

        public AgentState AgentState { get; private set; }

        public GoldBalanceState GoldBalanceState { get; private set; }

        public GoldBalanceState StakedBalanceState { get; private set; }

        public StakeState StakeState { get; private set; }

        public CrystalRandomSkillState CrystalRandomSkillState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public GameConfigState GameConfigState { get; private set; }

        public FungibleAssetValue CrystalBalance { get; private set; }

        public Dictionary<int, FungibleAssetValue> RuneStoneBalance { get; } = new();

        public int StakingLevel { get; private set; }

        private class Workshop
        {
            public Dictionary<int, CombinationSlotState> States { get; }= new();
        }

        private readonly Dictionary<Address, Workshop> _slotStates = new();

        private Dictionary<int, HammerPointState> _hammerPointStates;

        /// <summary>
        /// Hammer point state dictionary of current avatar.
        /// </summary>
        public IReadOnlyDictionary<int, HammerPointState> HammerPointStates => _hammerPointStates;

        public States()
        {
            DeselectAvatar();
        }

        #region Setter

        /// <summary>
        /// 에이전트 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// 최초로 할당하거나 기존과 다른 주소의 에이전트를 할당하면, 모든 아바타 상태를 새롭게 할당된다.
        /// </summary>
        /// <param name="state"></param>
        public async UniTask SetAgentStateAsync(AgentState state)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetAgentStateAsync)}] {nameof(state)} is null.");
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
                await AddOrReplaceAvatarStateAsync(pair.Value, pair.Key);
            }
        }

        public void SetGoldBalanceState(GoldBalanceState goldBalanceState)
        {
            if (goldBalanceState is null)
            {
                Debug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetGoldBalanceState)}] {nameof(goldBalanceState)} is null.");
                return;
            }

            GoldBalanceState = LocalLayer.Instance.Modify(goldBalanceState);
            AgentStateSubject.OnNextGold(GoldBalanceState.Gold);
        }

        public void SetCrystalBalance(FungibleAssetValue fav)
        {
            if (!fav.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                Debug.LogWarning($"Currency not matches. {fav.Currency}");
                return;
            }

            CrystalBalance = LocalLayer.Instance.ModifyCrystal(fav);
            AgentStateSubject.OnNextCrystal(CrystalBalance);
        }

        public async Task InitRuneStoneBalance()
        {
            RuneStoneBalance.Clear();
            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            var avatarAddress = CurrentAvatarState.address;
            var task = Task.Run(async () =>
            {
                var runes = new List<FungibleAssetValue>();
                await foreach (var row in runeSheet.Values)
                {
                    var rune = RuneHelper.ToCurrency(row, 0, null);
                    var fungibleAsset = await Game.Game.instance.Agent.GetBalanceAsync(avatarAddress, rune);
                    RuneStoneBalance.Add(row.Id, fungibleAsset);
                }

                return runes;
            });

            await task;
        }

        public async Task<FungibleAssetValue?> SetRuneStoneBalance(int runeId)
        {
            var avatarAddress = CurrentAvatarState.address;
            var costSheet = Game.Game.instance.TableSheets.RuneCostSheet;
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                return null;
            }

            var runeStoneId = costRow.Cost.First().RuneStoneId;
            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            var runeStone = runeSheet.Values.First(x => x.Id == runeStoneId);
            var rune = RuneHelper.ToCurrency(runeStone, 0, null);
            var fungibleAsset = await Game.Game.instance.Agent.GetBalanceAsync(avatarAddress, rune);
            RuneStoneBalance[runeStone.Id] = fungibleAsset;
            return fungibleAsset;
        }

        public void SetMonsterCollectionState(
            MonsterCollectionState monsterCollectionState,
            GoldBalanceState stakedBalanceState,
            int level)
        {
            if (monsterCollectionState is null)
            {
                Debug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetMonsterCollectionState)}] {nameof(monsterCollectionState)} is null.");
                return;
            }

            StakingLevel = level;
            StakedBalanceState = stakedBalanceState;
            MonsterCollectionStateSubject.OnNextLevel(StakingLevel);
        }

        public void SetStakeState(StakeState stakeState, GoldBalanceState stakedBalanceState, int stakingLevel)
        {
            if (stakeState is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetStakeState)}] {nameof(stakeState)} is null.");
                return;
            }

            StakeState = stakeState;
            StakedBalanceState = stakedBalanceState;
            StakingLevel = stakingLevel;
            MonsterCollectionStateSubject.OnNextLevel(stakingLevel);
        }

        public void SetCrystalRandomSkillState(CrystalRandomSkillState skillState)
        {
            if (skillState is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(SetCrystalRandomSkillState)}] {nameof(skillState)} is null.");
                return;
            }

            CrystalRandomSkillState = skillState;
        }

        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            Address avatarAddress,
            int index,
            bool initializeReactiveState = true)
        {
            var (exist, avatarState) = await TryGetAvatarStateAsync(avatarAddress, true);
            if (exist)
            {
                await AddOrReplaceAvatarStateAsync(avatarState, index, initializeReactiveState);
            }

            return null;
        }

        public static async UniTask<(bool exist, AvatarState avatarState)> TryGetAvatarStateAsync(
            Address address,
            bool allowBrokenState = false)
        {
            AvatarState avatarState = null;
            var exist = false;
            try
            {
                avatarState = await GetAvatarStateAsync(address, allowBrokenState);
                exist = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{e.GetType().FullName}: {e.Message} address({address.ToHex()})\n{e.StackTrace}");
            }

            return (exist, avatarState);
        }

        private static async UniTask<AvatarState> GetAvatarStateAsync(Address address, bool allowBrokenState)
        {
            var agent = Game.Game.instance.Agent;
            var avatarStateValue = await agent.GetStateAsync(address);
            if (avatarStateValue is not Dictionary dict)
            {
                Debug.LogWarning("Failed to get AvatarState");
                throw new FailedLoadStateException($"Failed to get AvatarState: {address.ToHex()}");
            }

            if (dict.ContainsKey(LegacyNameKey))
            {
                return new AvatarState(dict);
            }

            var addressPairList = new List<string>
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey
            }.Select(key => (Key: key, KeyAddress: address.Derive(key))).ToArray();

            var states = await agent.GetStateBulk(addressPairList.Select(value => value.KeyAddress));
            // Make Tuple list by state value and state address key.
            var stateAndKeys = states
                .Join(addressPairList,
                    state => state.Key,
                    addressPair => addressPair.KeyAddress,
                    (state, addressPair) => (state.Value, addressPair.Key));

            foreach (var (stateIValue, key) in stateAndKeys)
            {
                if (stateIValue is null)
                {
                    if (allowBrokenState && dict.ContainsKey(key))
                    {
                        dict = new Dictionary(dict.Remove((Text)key));
                    }

                    continue;
                }

                dict = dict.SetItem(key, stateIValue);
            }

            return new AvatarState(dict);
        }

        /// <summary>
        /// 아바타 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(AvatarState state, int index,
            bool initializeReactiveState = true)
        {
            if (state is null)
            {
                Debug.LogWarning($"[{nameof(States)}.{nameof(AddOrReplaceAvatarStateAsync)}] {nameof(state)} is null.");
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

            if (index == CurrentAvatarKey)
            {
                return await UniTask.Run(async () => await SelectAvatarAsync(index, initializeReactiveState));
            }

            return state;
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
        public async UniTask<AvatarState> SelectAvatarAsync(
            int index,
            bool initializeReactiveState = true)
        {
            if (!_avatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            var isNewlySelected = CurrentAvatarKey != index;

            CurrentAvatarKey = index;
            var avatarState = _avatarStates[CurrentAvatarKey];
            LocalLayer.Instance.InitializeCurrentAvatarState(avatarState);
            UpdateCurrentAvatarState(avatarState, initializeReactiveState);
            var agent = Game.Game.instance.Agent;
            var worldIds =
                await agent.GetStateAsync(avatarState.address.Derive("world_ids"));
            var unlockedIds = worldIds != null && !(worldIds is Null)
                ? worldIds.ToList(StateExtensions.ToInteger)
                : new List<int>
                {
                    1,
                    GameConfig.MimisbrunnrWorldId,
                };
            Widget.Find<WorldMap>().SharedViewModel.UnlockedWorldIds = unlockedIds;

            if (isNewlySelected)
            {
                _hammerPointStates = null;
                await UniTask.Run(async () =>
                {
                    var (exist, curAvatarState) = await TryGetAvatarStateAsync(avatarState.address);
                    if (!exist)
                    {
                        return;
                    }

                    var avatarAddress = CurrentAvatarState.address;
                    var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
                    var skillStateIValue = await Game.Game.instance.Agent.GetStateAsync(skillStateAddress);
                    if (skillStateIValue is List serialized)
                    {
                        var skillState = new CrystalRandomSkillState(skillStateAddress, serialized);
                        SetCrystalRandomSkillState(skillState);
                    }
                    else
                    {
                        CrystalRandomSkillState = null;
                    }

                    await SetCombinationSlotStatesAsync(curAvatarState);
                    await AddOrReplaceAvatarStateAsync(curAvatarState, CurrentAvatarKey);
                });
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

        private async UniTask SetCombinationSlotStatesAsync(AvatarState avatarState)
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
                var stateValue = await Game.Game.instance.Agent.GetStateAsync(slotAddress);
                var state = new CombinationSlotState((Dictionary)stateValue);
                UpdateCombinationSlotState(avatarState.address, i, state);
            }
        }

        public void UpdateCombinationSlotState(
            Address avatarAddress,
            int index,
            CombinationSlotState state)
        {
            if (!_slotStates.ContainsKey(avatarAddress))
            {
                _slotStates.Add(avatarAddress, new Workshop());
            }

            var slots = _slotStates[avatarAddress];
            if (slots.States.ContainsKey(index))
            {
                slots.States[index] = state;
            }
            else
            {
                slots.States.Add(index, state);
            }
        }

        public Dictionary<int, CombinationSlotState> GetCombinationSlotState(
            AvatarState avatarState,
            long currentBlockIndex)
        {
            if (!_slotStates.ContainsKey(avatarState.address))
            {
                _slotStates.Add(avatarState.address, new Workshop());
            }

            var states = _slotStates[avatarState.address].States;
            return states.Where(x => !x.Value.Validate(avatarState, currentBlockIndex))
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
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
            {
                Debug.Log($"[{nameof(States)}] {nameof(UpdateCurrentAvatarState)}() initializeReactiveState: false");
                return;
            }

            ReactiveAvatarState.Initialize(CurrentAvatarState);
        }

        public void UpdateHammerPointStates(int recipeId, HammerPointState state)
        {
            if (Addresses.GetHammerPointStateAddress(
                    Instance.CurrentAvatarState.address,
                    recipeId) == state.Address)
            {
                if (_hammerPointStates.ContainsKey(recipeId))
                {
                    _hammerPointStates[recipeId] = state;
                }
                else
                {
                    _hammerPointStates.Add(recipeId, state);
                }
            }

            HammerPointStatesSubject.OnReplaceHammerPointState(recipeId, state);
        }

        public void UpdateHammerPointStates(IEnumerable<int> recipeIds)
        {
            UniTask.Run(async () =>
            {
                if (TableSheets.Instance.CrystalHammerPointSheet is null)
                {
                    return;
                }

                var hammerPointStateAddresses =
                    recipeIds.Select(recipeId =>
                            (Addresses.GetHammerPointStateAddress(
                                CurrentAvatarState.address,
                                recipeId), recipeId))
                        .ToList();
                var states =
                    await Game.Game.instance.Agent.GetStateBulk(
                        hammerPointStateAddresses.Select(tuple => tuple.Item1));
                var joinedStates = states.Join(
                    hammerPointStateAddresses,
                    state => state.Key,
                    tuple => tuple.Item1,
                    (state, tuple) => (state, tuple.recipeId));

                _hammerPointStates ??= new Dictionary<int, HammerPointState>();
                foreach (var tuple in joinedStates)
                {
                    var state = tuple.state.Value is List list
                        ? new HammerPointState(tuple.state.Key, list)
                        : new HammerPointState(tuple.state.Key, tuple.recipeId);
                    if (_hammerPointStates.ContainsKey(tuple.recipeId))
                    {
                        _hammerPointStates[tuple.recipeId] = state;
                    }
                    else
                    {
                        _hammerPointStates.Add(tuple.recipeId, state);
                    }

                    HammerPointStatesSubject.OnReplaceHammerPointState(tuple.recipeId, state);
                }
            }).Forget();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Action.State;
using Libplanet.Common;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Debug = UnityEngine.Debug;
using static Lib9c.SerializeKeys;
using StateExtensions = Nekoyume.Model.State.StateExtensions;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stake;
using Nekoyume.TableData;
using Nekoyume.UI;
using Event = Nekoyume.Game.Event;

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

        // NOTE: Staking Properties
        public GoldBalanceState StakedBalanceState { get; private set; }
        public StakeStateV2? StakeStateV2 { get; private set; }
        public int StakingLevel { get; private set; }
        public StakeRegularFixedRewardSheet StakeRegularFixedRewardSheet { get; private set; }
        public StakeRegularRewardSheet StakeRegularRewardSheet { get; private set; }
        // ~: Staking Properties

        public CrystalRandomSkillState CrystalRandomSkillState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public ConcurrentDictionary<string, FungibleAssetValue> CurrentAvatarBalances { get; } = new();

        public GameConfigState GameConfigState { get; private set; }

        public FungibleAssetValue CrystalBalance { get; private set; }

        public AllRuneState AllRuneState { get; private set; }

        public readonly Dictionary<int, Dictionary<BattleType, RuneSlotState>>
            RuneSlotStates = new();

        public readonly Dictionary<int, Dictionary<BattleType, ItemSlotState>>
            ItemSlotStates = new();

        public Dictionary<BattleType, RuneSlotState> CurrentRuneSlotStates { get; } = new();
        public Dictionary<BattleType, ItemSlotState> CurrentItemSlotStates { get; } = new();

        private class Workshop
        {
            public Dictionary<int, CombinationSlotState> States { get; } = new();
        }

        public PetStates PetStates { get; } = new();

        private readonly Dictionary<Address, Workshop> _slotStates = new();

        private Dictionary<int, HammerPointState> _hammerPointStates;

        /// <summary>
        /// Hammer point state dictionary of current avatar.
        /// </summary>
        public IReadOnlyDictionary<int, HammerPointState> HammerPointStates => _hammerPointStates;

        public Address? PatronAddress { get; private set; }
        public bool PledgeRequested => PatronAddress is not null;
        public bool PledgeApproved { get; private set; }

        public CollectionState CollectionState { get; private set; }

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
                NcDebug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetAgentStateAsync)}] {nameof(state)} is null.");
                return;
            }

            var getAllOfAvatarStates = AgentState is null ||
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
                NcDebug.LogWarning(
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
                NcDebug.LogWarning($"Currency not matches. {fav.Currency}");
                return;
            }

            CrystalBalance = LocalLayer.Instance.ModifyCrystal(fav);
            AgentStateSubject.OnNextCrystal(CrystalBalance);
        }

        public async UniTask InitAvatarBalancesAsync()
        {
            await UniTask.WhenAll(
                UniTask.Run(async () =>
                {
                    var agent = Game.Game.instance.Agent;
                    var avatarAddress = CurrentAvatarState.address;
                    var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                    await foreach (var row in runeSheet.Values)
                    {
                        CurrentAvatarBalances.TryRemove(row.Ticker, out _);
                        var rune = RuneHelper.ToCurrency(row);
                        var fungibleAsset = await agent.GetBalanceAsync(avatarAddress, rune);
                        CurrentAvatarBalances.TryAdd(row.Ticker, fungibleAsset);
                    }
                }),
                UniTask.Run(async () =>
                {
                    var agent = Game.Game.instance.Agent;
                    var avatarAddress = CurrentAvatarState.address;
                    var petSheet = Game.Game.instance.TableSheets.PetSheet;
                    await foreach (var row in petSheet.Values)
                    {
                        CurrentAvatarBalances.TryRemove(row.SoulStoneTicker, out _);
                        var soulStone = PetHelper.GetSoulstoneCurrency(row.SoulStoneTicker);
                        var fungibleAsset = await agent.GetBalanceAsync(avatarAddress, soulStone);
                        CurrentAvatarBalances.TryAdd(soulStone.Ticker, fungibleAsset);
                    }
                }));
        }

        public void SetAllRuneState(AllRuneState allRuneState)
        {
            AllRuneState = allRuneState;
        }

        public async UniTask InitRuneSlotStates()
        {
            CurrentRuneSlotStates.Clear();
            CurrentRuneSlotStates.Add(
                BattleType.Adventure,
                new RuneSlotState(BattleType.Adventure));
            CurrentRuneSlotStates.Add(BattleType.Arena, new RuneSlotState(BattleType.Arena));
            CurrentRuneSlotStates.Add(BattleType.Raid, new RuneSlotState(BattleType.Raid));

            RuneSlotStates.Clear();
            foreach (var (index, avatarState) in _avatarStates)
            {
                RuneSlotStates.Add(index, new Dictionary<BattleType, RuneSlotState>());
                RuneSlotStates[index].Add(BattleType.Adventure,
                    new RuneSlotState(BattleType.Adventure));
                RuneSlotStates[index].Add(BattleType.Arena, new RuneSlotState(BattleType.Arena));
                RuneSlotStates[index].Add(BattleType.Raid, new RuneSlotState(BattleType.Raid));

                var addresses = new List<Address>
                {
                    RuneSlotState.DeriveAddress(avatarState.address, BattleType.Adventure),
                    RuneSlotState.DeriveAddress(avatarState.address, BattleType.Arena),
                    RuneSlotState.DeriveAddress(avatarState.address, BattleType.Raid)
                };
                var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, addresses);
                foreach (var value in stateBulk.Values)
                {
                    if (value is List list)
                    {
                        var slotState = new RuneSlotState(list);
                        RuneSlotStates[index][slotState.BattleType] = slotState;

                        if (avatarState.address == CurrentAvatarState.address)
                        {
                            CurrentRuneSlotStates[slotState.BattleType] = slotState;
                        }
                    }
                }
            }
        }

        public void UpdateRuneSlotState()
        {
            foreach (var runeSlotState in CurrentRuneSlotStates)
            {
                var states = CurrentRuneSlotStates[runeSlotState.Key].GetRuneSlot();
                foreach (var runeSlot in states)
                {
                    if (!runeSlot.RuneId.HasValue)
                    {
                        continue;
                    }

                    runeSlot.Equip(runeSlot.RuneId.Value);
                }
            }

            Event.OnUpdateRuneState.Invoke();
        }

        public void UpdateRuneSlotState(RuneSlotState slotState)
        {
            var slotIndex = AvatarStates
                .FirstOrDefault(x => x.Value.address == CurrentAvatarState.address).Key;
            CurrentRuneSlotStates[slotState.BattleType] = slotState;
            RuneSlotStates[slotIndex][slotState.BattleType] = slotState;
        }

        public async UniTask InitItemSlotStates()
        {
            CurrentItemSlotStates.Clear();
            CurrentItemSlotStates.Add(
                BattleType.Adventure,
                new ItemSlotState(BattleType.Adventure));
            CurrentItemSlotStates.Add(BattleType.Arena, new ItemSlotState(BattleType.Arena));
            CurrentItemSlotStates.Add(BattleType.Raid, new ItemSlotState(BattleType.Raid));

            ItemSlotStates.Clear();
            var agent = Game.Game.instance.Agent;
            foreach (var (index, avatarState) in _avatarStates)
            {
                ItemSlotStates.Add(index, new Dictionary<BattleType, ItemSlotState>());
                ItemSlotStates[index].Add(
                    BattleType.Adventure,
                    new ItemSlotState(BattleType.Adventure));
                ItemSlotStates[index].Add(BattleType.Arena, new ItemSlotState(BattleType.Arena));
                ItemSlotStates[index].Add(BattleType.Raid, new ItemSlotState(BattleType.Raid));

                var addresses = new List<Address>
                {
                    ItemSlotState.DeriveAddress(avatarState.address, BattleType.Adventure),
                    ItemSlotState.DeriveAddress(avatarState.address, BattleType.Arena),
                    ItemSlotState.DeriveAddress(avatarState.address, BattleType.Raid)
                };
                var stateBulk = await agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, addresses);
                foreach (var value in stateBulk.Values)
                {
                    if (value is List list)
                    {
                        var slotState = new ItemSlotState(list);
                        var checkedState = GetVerifiedItemSlotState(slotState, avatarState);
                        ItemSlotStates[index][checkedState.BattleType] = checkedState;
                        if (avatarState.address == CurrentAvatarState.address)
                        {
                            CurrentItemSlotStates[checkedState.BattleType] = checkedState;
                        }
                    }
                }
            }
        }

        private async UniTask InitItemSlotState(int slotIndex, AvatarState avatarState)
        {
            if (ItemSlotStates.ContainsKey(slotIndex))
            {
                return;
            }

            ItemSlotStates.Add(slotIndex, new Dictionary<BattleType, ItemSlotState>());
            ItemSlotStates[slotIndex].Add(
                BattleType.Adventure,
                new ItemSlotState(BattleType.Adventure));
            ItemSlotStates[slotIndex].Add(BattleType.Arena, new ItemSlotState(BattleType.Arena));
            ItemSlotStates[slotIndex].Add(BattleType.Raid, new ItemSlotState(BattleType.Raid));

            var addresses = new List<Address>
            {
                ItemSlotState.DeriveAddress(avatarState.address, BattleType.Adventure),
                ItemSlotState.DeriveAddress(avatarState.address, BattleType.Arena),
                ItemSlotState.DeriveAddress(avatarState.address, BattleType.Raid)
            };

            var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, addresses);
            foreach (var value in stateBulk.Values)
            {
                if (value is List list)
                {
                    var slotState = new ItemSlotState(list);
                    var checkedState = GetVerifiedItemSlotState(slotState, avatarState);
                    ItemSlotStates[slotIndex][checkedState.BattleType] = checkedState;
                }
            }
        }

        public void UpdateItemSlotState(ItemSlotState slotState)
        {
            var slotIndex = AvatarStates
                .FirstOrDefault(x => x.Value.address == CurrentAvatarState.address).Key;
            var checkedState = GetVerifiedItemSlotState(slotState, CurrentAvatarState);
            CurrentItemSlotStates[checkedState.BattleType] = checkedState;
            ItemSlotStates[slotIndex][checkedState.BattleType] = checkedState;
        }

        private static ItemSlotState GetVerifiedItemSlotState(
            ItemSlotState itemSlotState,
            AvatarState avatarState)
        {
            var agent = Game.Game.instance.Agent;
            var checkedItems = new List<Guid>();
            var items = avatarState.inventory.Items;
            foreach (var item in items)
            {
                if (item.item is Equipment equipment)
                {
                    if (itemSlotState.Equipments.Exists(x => x == equipment.ItemId))
                    {
                        if (!item.Locked)
                        {
                            var blockIndex = agent?.BlockIndex ?? -1;
                            if (equipment.RequiredBlockIndex <= blockIndex)
                            {
                                if (checkedItems.Contains(equipment.ItemId))
                                {
                                    NcDebug.LogError($"Duplicated ItemId in Inventory {equipment.ItemId}");
                                }
                                else
                                {
                                    checkedItems.Add(equipment.ItemId);
                                }
                            }
                        }
                    }
                }
            }

            itemSlotState.UpdateEquipment(checkedItems);
            return itemSlotState;
        }

        public void SetCurrentAvatarBalance(FungibleAssetValue fav)
        {
            CurrentAvatarBalances[fav.Currency.Ticker] = fav;
        }

        public void SetCurrentAvatarBalance<T>(ActionEvaluation<T> eval, Currency currency) where T : ActionBase
        {
            var fav = StateGetter.GetBalance(eval.OutputState, CurrentAvatarState.address, currency);
            SetCurrentAvatarBalance(fav);
        }

        public void SetStakeState(
            StakeStateV2? stakeStateV2,
            GoldBalanceState stakedBalanceState,
            int stakingLevel,
            [CanBeNull] StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet,
            [CanBeNull] StakeRegularRewardSheet stakeRegularRewardSheet)
        {
            StakedBalanceState = stakedBalanceState;
            StakingLevel = stakingLevel;
            StakeStateV2 = stakeStateV2;
            StakeRegularFixedRewardSheet = stakeRegularFixedRewardSheet;
            StakeRegularRewardSheet = stakeRegularRewardSheet;

            StakingSubject.OnNextLevel(StakingLevel);
            if (StakeStateV2.HasValue)
            {
                StakingSubject.OnNextStakeStateV2(StakeStateV2);
            }

            if (StakedBalanceState is not null)
            {
                StakingSubject.OnNextStakedNCG(StakedBalanceState.Gold);
            }

            if (StakeRegularRewardSheet is not null &&
                StakeRegularFixedRewardSheet is not null)
            {
                StakingSubject.OnNextStakeRegularFixedRewardSheet(StakeRegularFixedRewardSheet);
                StakingSubject.OnNextStakeRegularRewardSheet(StakeRegularRewardSheet);
            }
        }

        public void SetCrystalRandomSkillState(CrystalRandomSkillState skillState)
        {
            if (skillState is null)
            {
                NcDebug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetCrystalRandomSkillState)}] {nameof(skillState)} is null.");
            }

            CrystalRandomSkillState = skillState;
        }

        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            Address avatarAddress,
            int index,
            bool initializeReactiveState = true)
        {
            var avatarState =
                (await Game.Game.instance.Agent.GetAvatarStatesAsync(
                    new[] { avatarAddress }))[avatarAddress];
            if (avatarState is not null)
            {
                await AddOrReplaceAvatarStateAsync(avatarState, index, initializeReactiveState);
            }

            return null;
        }

        /// <summary>
        /// 아바타 상태를 할당한다.
        /// 로컬 세팅을 거친 상태가 최종적으로 할당된다.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="initializeReactiveState"></param>
        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            AvatarState state,
            int index,
            bool initializeReactiveState = true)
        {
            if (state is null)
            {
                NcDebug.LogWarning(
                    $"[{nameof(States)}.{nameof(AddOrReplaceAvatarStateAsync)}] {nameof(state)} is null.");
                return null;
            }

            if (AgentState is null || !AgentState.avatarAddresses.ContainsValue(state.address))
                throw new Exception(
                    $"`AgentState` is null or not found avatar's address({state.address}) in `AgentState`");

            state = LocalLayer.Instance.Modify(state);
            _avatarStates[index] = state;
            await InitItemSlotState(index, state);

            if (index == CurrentAvatarKey)
            {
                return await UniTask.RunOnThreadPool(async () =>
                    await SelectAvatarAsync(index, initializeReactiveState), configureAwait: false);
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
        /// <exception cref="KeyNotFoundException"></exception>
        public async UniTask<AvatarState> SelectAvatarAsync(
            int index,
            bool initializeReactiveState = true,
            bool forceNewSelection = false)
        {
            if (!_avatarStates.ContainsKey(index))
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            var isNewlySelected = forceNewSelection || CurrentAvatarKey != index;

            CurrentAvatarKey = index;
            var avatarState = _avatarStates[CurrentAvatarKey];
            LocalLayer.Instance.InitializeCurrentAvatarState(avatarState);
            UpdateCurrentAvatarState(avatarState, initializeReactiveState);
            var agent = Game.Game.instance.Agent;
            var worldIds = await agent.GetStateAsync(
                ReservedAddresses.LegacyAccount,
                avatarState.address.Derive("world_ids"));
            var unlockedIds = worldIds is not (null or Null)
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
                await UniTask.RunOnThreadPool(async () =>
                {
                    await InitializeAvatarAndRelatedStates(agent, avatarState.address);
                });

                Widget.Find<PatrolRewardPopup>().InitializePatrolReward().AsUniTask().Forget();
                Game.Game.instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
            }

            return CurrentAvatarState;
        }

        private async UniTask InitializeAvatarAndRelatedStates(IAgent agent, Address avatarAddr)
        {
            var curAvatarState = (await agent.GetAvatarStatesAsync(
                new[] { avatarAddr }))[avatarAddr];
            // AvatarState를 체인에서 가져오지 못했을때
            if (curAvatarState is null)
            {
                return;
            }

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddr);
            var combinationSlotAddresses = new List<Address>();
            for (var i = 0; i < 4; i++)
            {
                combinationSlotAddresses.Add(CombinationSlotState.DeriveAddress(curAvatarState.address, i));
            }

            var petIds = TableSheets.Instance.PetSheet.Values
                .Select(row => (row.Id, PetState.DeriveAddress(avatarAddr, row.Id)))
                .ToList();

            // [0]: combinationSlots
            // [1]: pet states
            var bulkStates = await Task.WhenAll(
                agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, combinationSlotAddresses),
                agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, petIds.Select(pair => pair.Item2))
            );
            LocalLayer.Instance.InitializeCombinationSlotsByCurrentAvatarState(curAvatarState);
            SetCombinationSlotStatesAsync(curAvatarState.address,
                combinationSlotAddresses.Select((address, i) =>
                    (i, new CombinationSlotState((Dictionary)bulkStates[0][address]))
                )
            );
            await AddOrReplaceAvatarStateAsync(curAvatarState, CurrentAvatarKey);
            SetPetStates(petIds.ToDictionary(pair => pair.Id, pair => bulkStates[1][pair.Item2]));

            // [0]: crystalRandomSkillState
            // [1]: CollectionState
            // [2]: ActionPoint
            // [3]: DailyRewardReceivedBlockIndex
            var listStates = await Task.WhenAll(
                agent.GetStateAsync(ReservedAddresses.LegacyAccount, skillStateAddress),
                agent.GetStateAsync(Addresses.Collection, avatarAddr),
                agent.GetStateAsync(Addresses.ActionPoint, avatarAddr),
                agent.GetStateAsync(Addresses.DailyReward, avatarAddr));
            SetCrystalRandomSkillState(listStates[0] is List serialized
                ? new CrystalRandomSkillState(skillStateAddress, serialized)
                : null);
            SetCollectionState(listStates[1] is List list
                ? new CollectionState(list)
                : new CollectionState());
            ReactiveAvatarState.UpdateActionPoint(listStates[2] is Integer ap
                ? ap
                : curAvatarState.actionPoint);
            ReactiveAvatarState.UpdateDailyRewardReceivedIndex(listStates[3] is Integer index
                ? index
                : curAvatarState.dailyRewardReceivedIndex);

            SetAllRuneState(await agent.GetAllRuneStateAsync(curAvatarState.address));

            await InitRuneSlotStates();
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

        private void SetCombinationSlotStatesAsync(Address avatarAddr,
            IEnumerable<(int, CombinationSlotState)> slotStates)
        {
            foreach (var slotState in slotStates)
            {
                UpdateCombinationSlotState(avatarAddr, slotState.Item1, slotState.Item2);
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
            slots.States[index] = state;
        }

        public Dictionary<int, CombinationSlotState> GetCombinationSlotState()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var states = _slotStates[CurrentAvatarState.address].States;
            return states.Where(x => !x.Value.ValidateV2(CurrentAvatarState, blockIndex))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
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
            return states.Where(x => !x.Value.ValidateV2(avatarState, currentBlockIndex))
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
        private void UpdateCurrentAvatarState(AvatarState state,
            bool initializeReactiveState = true)
        {
            CurrentAvatarState = state;

            if (!initializeReactiveState)
            {
                NcDebug.Log(
                    $"[{nameof(States)}] {nameof(UpdateCurrentAvatarState)}() initializeReactiveState: false");
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
                _hammerPointStates[recipeId] = state;
            }

            HammerPointStatesSubject.OnReplaceHammerPointState(recipeId, state);
        }

        public void UpdateHammerPointStates(IEnumerable<int> recipeIds)
        {
            UniTask.RunOnThreadPool(async () =>
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
                    await Game.Game.instance.Agent.GetStateBulkAsync(
                        ReservedAddresses.LegacyAccount,
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
                    _hammerPointStates[tuple.recipeId] = state;
                    HammerPointStatesSubject.OnReplaceHammerPointState(tuple.recipeId, state);
                }
            }).Forget();
        }

        public (List<Equipment>, List<Costume>) GetEquippedItems(BattleType battleType)
        {
            var itemSlotState = CurrentItemSlotStates[battleType];
            var avatarState = CurrentAvatarState;
            var equipmentInventory = avatarState.inventory.Equipments;
            var equipments = itemSlotState.Equipments
                .Select(guid => equipmentInventory.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumeInventory = avatarState.inventory.Costumes;
            var costumes = itemSlotState.Costumes
                .Select(guid => costumeInventory.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            return (equipments, costumes);
        }

        public List<RuneState> GetEquippedRuneStates(BattleType battleType)
        {
            return GetEquippedRuneStates(AllRuneState, battleType);
        }

        public List<RuneState> GetEquippedRuneStates(
            AllRuneState allRuneState,
            BattleType battleType)
        {
            var states = CurrentRuneSlotStates[battleType].GetRuneSlot();
            var runeStates = new List<RuneState>();
            foreach (var slot in states)
            {
                if (!slot.RuneId.HasValue)
                {
                    continue;
                }

                if (allRuneState.TryGetRuneState(slot.RuneId.Value, out var runeState))
                {
                    runeStates.Add(runeState);
                }
            }

            return runeStates;
        }

        private void SetPetStates(Dictionary<int,IValue> petRawStates)
        {
            foreach (var pair in petRawStates)
            {
                PetStates.UpdatePetState(
                    pair.Key,
                    pair.Value is List rawState ? new PetState(rawState) : null);
            }
        }

        public void SetCollectionState(CollectionState state)
        {
            if (state is null)
            {
                NcDebug.LogWarning($"[{nameof(States)}.{nameof(SetCollectionState)}] {nameof(state)} is null.");
            }

            CollectionState = state;
        }

        public void SetPledgeStates(Address? patronAddress, bool isApproved)
        {
            PatronAddress = patronAddress;
            PledgeApproved = isApproved;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Nekoyume.ApiClient;
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

        private readonly ConcurrentDictionary<int, AvatarState> _avatarStates = new();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public ConcurrentDictionary<string, FungibleAssetValue> CurrentAvatarBalances { get; } = new();

        public GameConfigState GameConfigState { get; private set; }

        public FungibleAssetValue CrystalBalance { get; private set; }

        public AllRuneState AllRuneState { get; private set; }
        
        public AllCombinationSlotState AllCombinationSlotState { get; private set; }

        public readonly ConcurrentDictionary<int, Dictionary<BattleType, RuneSlotState>>
            RuneSlotStates = new();

        public readonly ConcurrentDictionary<int, Dictionary<BattleType, ItemSlotState>>
            ItemSlotStates = new();

        public ConcurrentDictionary<BattleType, RuneSlotState> CurrentRuneSlotStates { get; } = new();
        public ConcurrentDictionary<BattleType, ItemSlotState> CurrentItemSlotStates { get; } = new();

        private class Workshop
        {
            private readonly ConcurrentDictionary<int, CombinationSlotState> _states = new();
            
            public void UpdateCombinationSlotState(int index, CombinationSlotState state)
            {
                _states[index] = state;
            }
            
            /// <summary>
            /// If you want to update the state, use <see cref="UpdateCombinationSlotState"/>.
            /// </summary>
            public IDictionary<int, CombinationSlotState> States => _states.ToImmutableSortedDictionary();
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

        public List<int> ClaimedGiftIds { get; private set; }

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
        
        private void SetAllCombinationSlotState(Address avatarAddress, AllCombinationSlotState allCombinationSlotState)
        {
            LocalLayer.Instance.InitializeCombinationSlots(allCombinationSlotState);
            AllCombinationSlotState = allCombinationSlotState;
            foreach (var slotState in allCombinationSlotState)
            {
                UpdateCombinationSlotState(avatarAddress, slotState.Index, slotState);
            }
        }

        public async UniTask InitRuneSlotStates()
        {
            CurrentRuneSlotStates.Clear();
            CurrentRuneSlotStates.TryAdd(
                BattleType.Adventure,
                new RuneSlotState(BattleType.Adventure));
            CurrentRuneSlotStates.TryAdd(BattleType.Arena, new RuneSlotState(BattleType.Arena));
            CurrentRuneSlotStates.TryAdd(BattleType.Raid, new RuneSlotState(BattleType.Raid));

            RuneSlotStates.Clear();
            foreach (var (index, avatarState) in _avatarStates)
            {
                RuneSlotStates.TryAdd(index, new Dictionary<BattleType, RuneSlotState>());
                RuneSlotStates[index].TryAdd(BattleType.Adventure,
                    new RuneSlotState(BattleType.Adventure));
                RuneSlotStates[index].TryAdd(BattleType.Arena, new RuneSlotState(BattleType.Arena));
                RuneSlotStates[index].TryAdd(BattleType.Raid, new RuneSlotState(BattleType.Raid));

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
            CurrentItemSlotStates.TryAdd(
                BattleType.Adventure,
                new ItemSlotState(BattleType.Adventure));
            CurrentItemSlotStates.TryAdd(BattleType.Arena, new ItemSlotState(BattleType.Arena));
            CurrentItemSlotStates.TryAdd(BattleType.Raid, new ItemSlotState(BattleType.Raid));

            ItemSlotStates.Clear();
            var agent = Game.Game.instance.Agent;
            foreach (var (index, avatarState) in _avatarStates)
            {
                ItemSlotStates.TryAdd(index, new Dictionary<BattleType, ItemSlotState>());
                ItemSlotStates[index].TryAdd(
                    BattleType.Adventure,
                    new ItemSlotState(BattleType.Adventure));
                ItemSlotStates[index].TryAdd(BattleType.Arena, new ItemSlotState(BattleType.Arena));
                ItemSlotStates[index].TryAdd(BattleType.Raid, new ItemSlotState(BattleType.Raid));

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

            ItemSlotStates.TryAdd(slotIndex, new Dictionary<BattleType, ItemSlotState>());
            ItemSlotStates[slotIndex].TryAdd(
                BattleType.Adventure,
                new ItemSlotState(BattleType.Adventure));
            ItemSlotStates[slotIndex].TryAdd(BattleType.Arena, new ItemSlotState(BattleType.Arena));
            ItemSlotStates[slotIndex].TryAdd(BattleType.Raid, new ItemSlotState(BattleType.Raid));

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
            {
                throw new Exception(
                    $"`AgentState` is null or not found avatar's address({state.address}) in `AgentState`");
            }

            state = LocalLayer.Instance.Modify(state);
            _avatarStates[index] = state;
            await InitItemSlotState(index, state);

            if (index == CurrentAvatarKey)
            {
                return await UniTask.RunOnThreadPool(async () =>
                    await SelectAvatarAsync(index, initializeReactiveState), false);
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
            {
                throw new KeyNotFoundException($"{nameof(index)}({index})");
            }

            _avatarStates.TryRemove(index, out _);

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
                    GameConfig.MimisbrunnrWorldId
                };
            Widget.Find<WorldMap>().SharedViewModel.UnlockedWorldIds = unlockedIds;

            if (isNewlySelected)
            {
                _hammerPointStates = null;
                await UniTask.RunOnThreadPool(async () => { await InitializeAvatarAndRelatedStates(agent, avatarState.address); });

                PatrolReward.InitializeInformation(avatarState.address.ToHex(),
                    AgentState.address.ToHex(), avatarState.level).AsUniTask().Forget();
                ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
                Widget.Find<CombinationSlotsPopup>().ClearSlots();
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

            var petIds = TableSheets.Instance.PetSheet.Values
                .Select(row => (row.Id, PetState.DeriveAddress(avatarAddr, row.Id)))
                .ToList();
            var petBulkState = await agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, petIds.Select(pair => pair.Item2));
            
            await AddOrReplaceAvatarStateAsync(curAvatarState, CurrentAvatarKey);
            SetPetStates(petIds.ToDictionary(pair => pair.Id, pair => petBulkState[pair.Item2]));

            // [0]: crystalRandomSkillState
            // [1]: CollectionState
            // [2]: ActionPoint
            // [3]: DailyRewardReceivedBlockIndex
            // [4]: Relationship
            // [5]: ClaimedGiftIds
            var listStates = await Task.WhenAll(
                agent.GetStateAsync(ReservedAddresses.LegacyAccount, skillStateAddress),
                agent.GetStateAsync(Addresses.Collection, avatarAddr),
                agent.GetStateAsync(Addresses.ActionPoint, avatarAddr),
                agent.GetStateAsync(Addresses.DailyReward, avatarAddr),
                agent.GetStateAsync(Addresses.Relationship, avatarAddr),
                agent.GetStateAsync(Addresses.ClaimedGiftIds, avatarAddr));
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
            ReactiveAvatarState.UpdateRelationship(listStates[4] is Integer proficiency
                ? proficiency
                : 0);
            SetClaimedGiftIds(listStates[5] is List rawIds
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>());

            var allCombinationSlotState = await agent.GetAllCombinationSlotStateAsync(curAvatarState.address);
            SetAllCombinationSlotState(avatarAddr, allCombinationSlotState);
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

        public void UpdateCombinationSlotState(
            Address avatarAddress,
            int index,
            CombinationSlotState state)
        {
            _slotStates.TryAdd(avatarAddress, new Workshop());

            var slots = _slotStates[avatarAddress];
            slots.UpdateCombinationSlotState(index, state);
        }

        public Dictionary<int, CombinationSlotState> GetUsedCombinationSlotState()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var states = _slotStates[CurrentAvatarState.address].States;
            return states.Where(x => !x.Value.ValidateV2(blockIndex))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        [CanBeNull]
        public Dictionary<int, CombinationSlotState> GetUsedCombinationSlotState(
            AvatarState avatarState,
            long currentBlockIndex)
        {
            if (!_slotStates.ContainsKey(avatarState.address))
            {
                return null;
            }

            var states = _slotStates[avatarState.address].States;
            return states.Where(x => !x.Value.ValidateV2(currentBlockIndex))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        [CanBeNull]
        public Dictionary<int, CombinationSlotState> GetAvailableCombinationSlotState(
            AvatarState avatarState,
            long currentBlockIndex)
        {
            if (!_slotStates.ContainsKey(avatarState.address))
            {
                return null;
            }

            var states = _slotStates[avatarState.address].States;
            return states.Where(x => x.Value.ValidateV2(currentBlockIndex))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        [CanBeNull]
        public IDictionary<int, CombinationSlotState> GetCombinationSlotState(AvatarState avatarState)
        {
            return !_slotStates.ContainsKey(avatarState.address) ? null : _slotStates[avatarState.address].States;
        }
        
        public void SetGameConfigState(GameConfigState state)
        {
            GameConfigState = state;
            GameConfigStateSubject.OnNext(state);
        }

#endregion

        /// <summary>
        /// AvatarState를 받아 Update하는 메소드입니다. 무조건 깨끗한 상태의 체인에서 받아온 AvatarState를 넣는걸 목적으로 합니다.
        /// </summary>
        public void UpdateCurrentAvatarState(AvatarState state,
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

        private void SetPetStates(Dictionary<int, IValue> petRawStates)
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

        private void SetClaimedGiftIds(List<int> claimedGiftIds)
        {
            ClaimedGiftIds = claimedGiftIds;
        }
    }
}

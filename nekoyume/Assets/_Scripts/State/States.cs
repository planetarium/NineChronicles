using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.State.Subjects;
using Debug = UnityEngine.Debug;
using static Lib9c.SerializeKeys;
using StateExtensions = Nekoyume.Model.State.StateExtensions;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
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

        public GoldBalanceState StakedBalanceState { get; private set; }

        public StakeState StakeState { get; private set; }

        public CrystalRandomSkillState CrystalRandomSkillState { get; private set; }

        private readonly Dictionary<int, AvatarState> _avatarStates = new();

        public IReadOnlyDictionary<int, AvatarState> AvatarStates => _avatarStates;

        public int CurrentAvatarKey { get; private set; }

        public AvatarState CurrentAvatarState { get; private set; }

        public Dictionary<string, FungibleAssetValue> CurrentAvatarBalances { get; } = new();

        public GameConfigState GameConfigState { get; private set; }

        public FungibleAssetValue CrystalBalance { get; private set; }

        public List<RuneState> RuneStates { get; } = new();

        public readonly Dictionary<int, Dictionary<BattleType, RuneSlotState>>
            RuneSlotStates = new();

        public readonly Dictionary<int, Dictionary<BattleType, ItemSlotState>>
            ItemSlotStates = new();

        public Dictionary<BattleType, RuneSlotState> CurrentRuneSlotStates { get; } = new();
        public Dictionary<BattleType, ItemSlotState> CurrentItemSlotStates { get; } = new();

        public int StakingLevel { get; private set; }

        public GrandFinaleStates GrandFinaleStates { get; } = new();

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
                Debug.LogWarning(
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

        public async UniTask InitAvatarBalancesAsync()
        {
            await UniTask.WhenAll(
                InitRuneStoneBalance(),
                InitSoulStoneBalance());
        }

        public async UniTask InitRuneStoneBalance()
        {
            var agent = Game.Game.instance.Agent;
            var avatarAddress = CurrentAvatarState.address;
            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            await foreach (var row in runeSheet.Values)
            {
                CurrentAvatarBalances.Remove(row.Ticker);
                var rune = RuneHelper.ToCurrency(row);
                var fungibleAsset = await agent.GetBalanceAsync(avatarAddress, rune);
                CurrentAvatarBalances.Add(row.Ticker, fungibleAsset);
            }
        }

        public async UniTask InitRuneStates()
        {
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var avatarAddress = CurrentAvatarState.address;
            var runeIds = runeListSheet.Values.Select(x => x.Id).ToList();
            var runeAddresses = runeIds.Select(id => RuneState.DeriveAddress(avatarAddress, id))
                .ToList();
            var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(runeAddresses);
            RuneStates.Clear();
            foreach (var value in stateBulk.Values)
            {
                if (value is List list)
                {
                    RuneStates.Add(new RuneState(list));
                }
            }
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
                var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(addresses);
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

        public async Task UpdateRuneSlotStates(BattleType battleType)
        {
            var avatarAddress = CurrentAvatarState.address;
            var address = RuneSlotState.DeriveAddress(avatarAddress, battleType);
            var value = await Game.Game.instance.Agent.GetStateAsync(address);
            if (value is List list)
            {
                var slotState = new RuneSlotState(list);
                CurrentRuneSlotStates[slotState.BattleType] = slotState;
                var slotIndex = AvatarStates.FirstOrDefault(x =>
                    x.Value.address == avatarAddress).Key;
                RuneSlotStates[slotIndex][battleType] = slotState;
            }
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
                var stateBulk = await agent.GetStateBulkAsync(addresses);
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

        public async UniTask InitSoulStoneBalance()
        {
            var agent = Game.Game.instance.Agent;
            var avatarAddress = CurrentAvatarState.address;
            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            await foreach (var row in petSheet.Values)
            {
                CurrentAvatarBalances.Remove(row.SoulStoneTicker);
                var soulStone = PetHelper.GetSoulstoneCurrency(row.SoulStoneTicker);
                var fungibleAsset = await agent.GetBalanceAsync(avatarAddress, soulStone);
                CurrentAvatarBalances.Add(soulStone.Ticker, fungibleAsset);
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

            var stateBulk = await Game.Game.instance.Agent.GetStateBulkAsync(addresses);
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

        public async Task UpdateItemSlotStates(BattleType battleType)
        {
            var avatarAddress = CurrentAvatarState.address;
            var address = ItemSlotState.DeriveAddress(avatarAddress, battleType);
            var value = await Game.Game.instance.Agent.GetStateAsync(address);
            if (value is List list)
            {
                var slotState = new ItemSlotState(list);
                var checkedState = GetVerifiedItemSlotState(slotState, CurrentAvatarState);
                CurrentItemSlotStates[checkedState.BattleType] = checkedState;
                var slotIndex = AvatarStates.FirstOrDefault(x =>
                    x.Value.address == avatarAddress).Key;
                ItemSlotStates[slotIndex][battleType] = checkedState;
            }
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
                                checkedItems.Add(equipment.ItemId);
                            }
                        }
                    }
                }
            }

            itemSlotState.UpdateEquipment(checkedItems);
            return itemSlotState;
        }

        public async Task<FungibleAssetValue?> SetRuneStoneBalance(int runeId)
        {
            var avatarAddress = CurrentAvatarState.address;
            var costSheet = Game.Game.instance.TableSheets.RuneCostSheet;
            if (!costSheet.TryGetValue(runeId, out _))
            {
                return null;
            }

            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            var runeRow = runeSheet.Values.First(x => x.Id == runeId);
            var rune = RuneHelper.ToCurrency(runeRow);
            var fungibleAsset = await Game.Game.instance.Agent.GetBalanceAsync(avatarAddress, rune);
            CurrentAvatarBalances[runeRow.Ticker] = fungibleAsset;
            return fungibleAsset;
        }

        public async Task SetBalanceAsync(string ticker)
        {
            var currency = Currencies.GetMinterlessCurrency(ticker);
            var agent = Game.Game.instance.Agent;
            var fungibleAsset = await agent.GetBalanceAsync(CurrentAvatarState.address, currency);
            CurrentAvatarBalances[ticker] = fungibleAsset;
        }

        /// <summary>
        /// For caching
        /// </summary>
        public void SetCurrentAvatarBalance(FungibleAssetValue fav)
        {
            var preFav = CurrentAvatarBalances[fav.Currency.Ticker];
            var major = preFav.MajorUnit - fav.MajorUnit;
            var miner = preFav.MinorUnit - fav.MinorUnit;
            CurrentAvatarBalances[fav.Currency.Ticker] =
                new FungibleAssetValue(fav.Currency, major, miner);
        }

        public void SetStakeState(
            StakeState stakeState,
            GoldBalanceState stakedBalanceState,
            int stakingLevel)
        {
            if (stakeState is null)
            {
                Debug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetStakeState)}] {nameof(stakeState)} is null.");
                return;
            }

            StakeState = stakeState;
            StakedBalanceState = stakedBalanceState;
            StakingLevel = stakingLevel;
            StakingLevelSubject.OnNextLevel(stakingLevel);
        }

        public void SetCrystalRandomSkillState(CrystalRandomSkillState skillState)
        {
            if (skillState is null)
            {
                Debug.LogWarning(
                    $"[{nameof(States)}.{nameof(SetCrystalRandomSkillState)}] {nameof(skillState)} is null.");
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
                Debug.LogWarning(
                    $"{e.GetType().FullName}: {e.Message} address({address.ToHex()})\n{e.StackTrace}");
            }

            return (exist, avatarState);
        }

        private static async UniTask<AvatarState> GetAvatarStateAsync(Address address,
            bool allowBrokenState)
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

            var states =
                await agent.GetStateBulkAsync(addressPairList.Select(value => value.KeyAddress));
            // Make Tuple list by state value and state address key.
            var stateAndKeys = states.Join(
                addressPairList,
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
        public async UniTask<AvatarState> AddOrReplaceAvatarStateAsync(
            AvatarState state,
            int index,
            bool initializeReactiveState = true)
        {
            if (state is null)
            {
                Debug.LogWarning(
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
                return await UniTask.Run(async () =>
                    await SelectAvatarAsync(index, initializeReactiveState));
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
            var worldIds = await agent.GetStateAsync(avatarState.address.Derive("world_ids"));
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
                await UniTask.Run(async () =>
                {
                    var (exist, curAvatarState) = await TryGetAvatarStateAsync(avatarState.address);
                    if (!exist)
                    {
                        return;
                    }

                    var avatarAddress = CurrentAvatarState.address;
                    var skillStateAddress =
                        Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddress);
                    var skillStateIValue =
                        await Game.Game.instance.Agent.GetStateAsync(skillStateAddress);
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
                    await SetPetStates(avatarState.address);
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
            var agent = Game.Game.instance.Agent;
            for (var i = 0; i < avatarState.combinationSlotAddresses.Count; i++)
            {
                var slotAddress = avatarState.address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                var stateValue = await agent.GetStateAsync(slotAddress);
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
            slots.States[index] = state;
        }

        public Dictionary<int, CombinationSlotState> GetCombinationSlotState()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var states = _slotStates[CurrentAvatarState.address].States;
            return states.Where(x => !x.Value.Validate(CurrentAvatarState, blockIndex))
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
        private void UpdateCurrentAvatarState(AvatarState state,
            bool initializeReactiveState = true)
        {
            CurrentAvatarState = state;

            if (!initializeReactiveState)
            {
                Debug.Log(
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
                    await Game.Game.instance.Agent.GetStateBulkAsync(
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
            var states = CurrentRuneSlotStates[battleType].GetRuneSlot();
            var runeStates = new List<RuneState>();
            foreach (var slot in states)
            {
                if (!slot.RuneId.HasValue)
                {
                    continue;
                }

                var runeState = RuneStates.FirstOrDefault(x => x.RuneId == slot.RuneId);
                if (runeState != null)
                {
                    runeStates.Add(runeState);
                }
            }

            return runeStates;
        }

        public bool TryGetRuneState(int runeId, out RuneState runeState)
        {
            runeState = RuneStates.FirstOrDefault(x => x.RuneId == runeId);
            return runeState != null;
        }

        private async UniTask SetPetStates(Address avatarAddress)
        {
            var petIds = TableSheets.Instance.PetSheet.Values.Select(row => row.Id).ToList();
            var petRawStates = await Game.Game.instance.Agent.GetStateBulkAsync(
                petIds.Select(id => PetState.DeriveAddress(avatarAddress, id))
            );
            foreach (var petId in petIds)
            {
                var petAddress = PetState.DeriveAddress(avatarAddress, petId);
                PetStates.UpdatePetState(
                    petId,
                    petRawStates[petAddress] is List rawState ? new PetState(rawState) : null);
            }
        }

        public void SetPledgeStates(Address? patronAddress, bool isApproved)
        {
            PatronAddress = patronAddress;
            PledgeApproved = isApproved;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using Nekoyume.Helper;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
#endif

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("create_avatar")]
    public class CreateAvatar0 : GameAction, ICreateAvatarV1
    {
        public Address avatarAddress;
        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        Address ICreateAvatarV1.AvatarAddress => avatarAddress;
        int ICreateAvatarV1.Index => index;
        int ICreateAvatarV1.Hair => hair;
        int ICreateAvatarV1.Lens => lens;
        int ICreateAvatarV1.Ear => ear;
        int ICreateAvatarV1.Tail => tail;
        string ICreateAvatarV1.Name => name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["index"] = (Integer) index,
            ["hair"] = (Integer) hair,
            ["lens"] = (Integer) lens,
            ["ear"] = (Integer) ear,
            ["tail"] = (Integer) tail,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            index = (int) ((Integer) plainValue["index"]).Value;
            hair = (int) ((Integer) plainValue["hair"]).Value;
            lens = (int) ((Integer) plainValue["lens"]).Value;
            ear = (int) ((Integer) plainValue["ear"]).Value;
            tail = (int) ((Integer) plainValue["tail"]).Value;
            name = (Text) plainValue["name"];
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ctx.Signer, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat,
                            i
                        )
                    );
                    states = states.SetState(slotAddress, MarkChanged);
                }

                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(Addresses.Ranking, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address, context.Signer);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            Log.Warning("{AddressesHex}create_avatar is deprecated. Please use create_avatar2", addressesHex);

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"{addressesHex}Aborted as the input name {name} does not follow the allowed name pattern.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}CreateAvatar exec started", addressesHex);
            AgentState existingAgentState = states.GetAgentState(ctx.Signer);
            var agentState = existingAgentState ?? new AgentState(ctx.Signer);
            var avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (!(0 <= index && index < GameConfig.SlotCount))
            {
                throw new AvatarIndexOutOfRangeException(
                    $"{addressesHex}Aborted as the index is out of range #{index}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"{addressesHex}Aborted as the signer already has an avatar at index #{index}.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose("{AddressesHex}Execute CreateAvatar; player: {AvatarAddress}", addressesHex, avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = ctx.PreviousStates.GetSheet<MaterialItemSheet>();

            var rankingState = ctx.PreviousStates.GetRankingState0();

            var rankingMapAddress = rankingState.UpdateRankingMap(avatarAddress);

            avatarState = CreateAvatarState(name, avatarAddress, ctx, materialItemSheet, rankingMapAddress);

            if (hair < 0) hair = 0;
            if (lens < 0) lens = 0;
            if (ear < 0) ear = 0;
            if (tail < 0) tail = 0;

            avatarState.Customize(hair, lens, ear, tail);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(address, GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                states = states.SetState(address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards2(materialItemSheet);

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(Addresses.Ranking, rankingState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
        }

        public static AvatarState CreateAvatarState(string name,
            Address avatarAddress,
            IActionContext ctx,
            MaterialItemSheet materialItemSheet,
            Address rankingMapAddress)
        {
            var state = ctx.PreviousStates;
            var gameConfigState = state.GetGameConfigState();
            var avatarState = new AvatarState(
                avatarAddress,
                ctx.Signer,
                ctx.BlockIndex,
                state.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress,
                name
            );

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
            var costumeItemSheet = ctx.PreviousStates.GetSheet<CostumeItemSheet>();
            var equipmentItemSheet = ctx.PreviousStates.GetSheet<EquipmentItemSheet>();
            var consumableItemSheet = ctx.PreviousStates.GetSheet<ConsumableItemSheet>();
            AddItemsForTest(
                avatarState: avatarState,
                random: ctx.Random,
                costumeItemSheet: costumeItemSheet,
                materialItemSheet: materialItemSheet,
                equipmentItemSheet: equipmentItemSheet,
                consumableItemSheet: consumableItemSheet,
                data.MaterialCount,
                data.TradableMaterialCount,
                data.FoodCount);

            var skillSheet = ctx.PreviousStates.GetSheet<SkillSheet>();
            var optionSheet = ctx.PreviousStates.GetSheet<EquipmentItemOptionSheet>();

            var items = data.CustomEquipmentItems;
            foreach (var item in items)
            {
                AddCustomEquipment(
                    avatarState: avatarState,
                    random: ctx.Random,
                    skillSheet: skillSheet,
                    equipmentItemSheet: equipmentItemSheet,
                    equipmentItemOptionSheet: optionSheet,
                    // Set level of equipment here.
                    level: item.Level,
                    // Set recipeId of target equipment here.
                    recipeId: item.ID,
                    // Add optionIds here.
                    item.OptionIds);
            }
#endif

            return avatarState;
        }

        private static void AddItemsForTest(
            AvatarState avatarState,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            MaterialItemSheet materialItemSheet,
            EquipmentItemSheet equipmentItemSheet,
            ConsumableItemSheet consumableItemSheet,
            int materialCount,
            int tradableMaterialCount,
            int foodCount)
        {
            foreach (var row in costumeItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem2(ItemFactory.CreateCostume(row, random.GenerateRandomGuid()));
            }

            foreach (var row in materialItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem2(ItemFactory.CreateMaterial(row), materialCount);

                if (row.ItemSubType == ItemSubType.Hourglass ||
                    row.ItemSubType == ItemSubType.ApStone)
                {
                    avatarState.inventory.AddItem2(ItemFactory.CreateTradableMaterial(row), tradableMaterialCount);
                }
            }

            foreach (var row in equipmentItemSheet.OrderedList.Where(row =>
                row.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem2(ItemFactory.CreateItemUsable(row, itemId, default));
            }

            foreach (var row in consumableItemSheet.OrderedList)
            {
                for (var i = 0; i < foodCount; i++)
                {
                    var itemId = random.GenerateRandomGuid();
                    var consumable = (Consumable)ItemFactory.CreateItemUsable(row, itemId,
                        0, 0);
                    avatarState.inventory.AddItem2(consumable);
                }
            }
        }

        private static void AddCustomEquipment(
            AvatarState avatarState,
            IRandom random,
            SkillSheet skillSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            int level,
            int recipeId,
            params int[] optionIds
            )
        {
            if (!equipmentItemSheet.TryGetValue(recipeId, out var equipmentRow))
            {
                return;
            }

            var itemId = random.GenerateRandomGuid();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, itemId, 0, level);
            var optionRows = new List<EquipmentItemOptionSheet.Row>();
            foreach (var optionId in optionIds)
            {
                if (!equipmentItemOptionSheet.TryGetValue(optionId, out var optionRow))
                {
                    continue;
                }
                optionRows.Add(optionRow);
            }

            AddOption(skillSheet, equipment, optionRows, random);

            avatarState.inventory.AddItem2(equipment);
        }

        private static HashSet<int> AddOption(
            SkillSheet skillSheet,
            Equipment equipment,
            IEnumerable<EquipmentItemOptionSheet.Row> optionRows,
            IRandom random)
        {
            var optionIds = new HashSet<int>();

            foreach (var optionRow in optionRows.OrderBy(r => r.Id))
            {
                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                    }
                }

                optionIds.Add(optionRow.Id);
            }

            return optionIds;
        }

        public static IAccountStateDelta AddRunesForTest(
            Address avatarAddress,
            IAccountStateDelta states)
        {
            var runeSheet = states.GetSheet<RuneSheet>();
            foreach (var row in runeSheet.Values)
            {
                var rune = RuneHelper.ToFungibleAssetValue(row, int.MaxValue);
                states = states.MintAsset(avatarAddress, rune);
            }
            return states;
        }
    }
}

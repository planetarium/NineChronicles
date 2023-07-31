using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Nekoyume.TableData.Pet;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1711
    /// </summary>
    [Serializable]
    [ActionType("combination_equipment16")]
    public class CombinationEquipment : GameAction, ICombinationEquipmentV4
    {
        public const string AvatarAddressKey = "a";
        public Address avatarAddress;

        public const string SlotIndexKey = "s";
        public int slotIndex;

        public const string RecipeIdKey = "r";
        public int recipeId;

        public const string SubRecipeIdKey = "i";
        public int? subRecipeId;
        public const string PayByCrystalKey = "p";
        public bool payByCrystal;
        public const string UseHammerPointKey = "h";
        public bool useHammerPoint;
        public const string PetIdKey = "pid";
        public int? petId;

        public const int BasicSubRecipeHammerPoint = 1;
        public const int SpecialSubRecipeHammerPoint = 2;

        Address ICombinationEquipmentV4.AvatarAddress => avatarAddress;
        int ICombinationEquipmentV4.RecipeId => recipeId;
        int ICombinationEquipmentV4.SlotIndex => slotIndex;
        int? ICombinationEquipmentV4.SubRecipeId => subRecipeId;
        bool ICombinationEquipmentV4.PayByCrystal => payByCrystal;
        bool ICombinationEquipmentV4.UseHammerPoint => useHammerPoint;
        int? ICombinationEquipmentV4.PetId => petId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
                [SubRecipeIdKey] = subRecipeId.Serialize(),
                [PayByCrystalKey] = payByCrystal.Serialize(),
                [UseHammerPointKey] = useHammerPoint.Serialize(),
                [PetIdKey] = petId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            slotIndex = plainValue[SlotIndexKey].ToInteger();
            recipeId = plainValue[RecipeIdKey].ToInteger();
            subRecipeId = plainValue[SubRecipeIdKey].ToNullableInteger();
            payByCrystal = plainValue[PayByCrystalKey].ToBoolean();
            useHammerPoint = plainValue[UseHammerPointKey].ToBoolean();
            petId = plainValue[PetIdKey].ToNullableInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CombinationEquipment exec started", addressesHex);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out var agentState,
                    out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Validate Required Cleared Tutorial Stage
            if (!avatarState.worldInformation.IsStageCleared(
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction,
                    current);
            }
            // ~Validate Required Cleared Tutorial Stage

            // Validate SlotIndex
            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if (!slotState.Validate(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }
            // ~Validate SlotIndex

            // Validate PetState
            PetState petState = null;
            if (petId.HasValue)
            {
                var petStateAddress = PetState.DeriveAddress(avatarAddress, petId.Value);
                if (!states.TryGetState(petStateAddress, out List rawState))
                {
                    throw new FailedLoadStateException($"{addressesHex}Aborted as the {nameof(PetState)} was failed to load.");
                }
                petState = new PetState(rawState);

                if (!petState.Validate(context.BlockIndex))
                {
                    throw new PetIsLockedException($"{addressesHex}Aborted as the pet is already in use.");
                }
            }
            // ~Validate PetState

            // Validate Work
            var costActionPoint = 0;
            var costNcg = 0L;
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(SkillSheet),
                typeof(CrystalMaterialCostSheet),
                typeof(CrystalFluctuationSheet),
                typeof(CrystalHammerPointSheet),
                typeof(ConsumableItemRecipeSheet),
            });

            // Validate RecipeId
            var equipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
            if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(EquipmentItemRecipeSheet),
                    recipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe ResultEquipmentId
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(equipmentItemSheet),
                    recipeRow.ResultEquipmentId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            if (!materialItemSheet.TryGetValue(recipeRow.MaterialId, out var materialRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(MaterialItemSheet),
                    recipeRow.MaterialId);
            }

            if (requiredFungibleItems.ContainsKey(materialRow.Id))
            {
                requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
            }
            else
            {
                requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
            }
            // ~Validate Recipe Material

            // Validate Recipe Unlocked.
            if (equipmentItemRecipeSheet[recipeId].CRYSTAL != 0)
            {
                var unlockedRecipeIdsAddress = avatarAddress.Derive("recipe_ids");
                if (!states.TryGetState(unlockedRecipeIdsAddress, out List rawIds))
                {
                    throw new FailedLoadStateException("can't find UnlockedRecipeList.");
                }

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);
                if (!unlockedIds.Contains(recipeId))
                {
                    throw new InvalidRecipeIdException($"unlock {recipeId} first.");
                }

                if (!avatarState.worldInformation.IsStageCleared(recipeRow.UnlockStage))
                {
                    avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                    throw new NotEnoughClearedStageLevelException(
                        addressesHex,
                        recipeRow.UnlockStage,
                        current);
                }
            }
            // ~Validate Recipe Unlocked

            // Validate SubRecipeId
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow = null;
            if (subRecipeId.HasValue)
            {
                if (!recipeRow.SubRecipeIds.Contains(subRecipeId.Value))
                {
                    throw new SheetRowColumnException(
                        $"{addressesHex}Aborted as the sub recipe {subRecipeId.Value} was failed to load from the sheet."
                    );
                }

                var equipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
                if (!equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId.Value, out subRecipeRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemSubRecipeSheetV2),
                        subRecipeId.Value);
                }

                // Validate SubRecipe Material
                for (var i = subRecipeRow.Materials.Count; i > 0; i--)
                {
                    var materialInfo = subRecipeRow.Materials[i - 1];
                    if (!materialItemSheet.TryGetValue(materialInfo.Id, out materialRow))
                    {
                        throw new SheetRowNotFoundException(
                            addressesHex,
                            nameof(MaterialItemSheet),
                            materialInfo.Id);
                    }

                    if (requiredFungibleItems.ContainsKey(materialRow.Id))
                    {
                        requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                    }
                    else
                    {
                        requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                    }
                }
                // ~Validate SubRecipe Material

                costActionPoint += subRecipeRow.RequiredActionPoint;
                costNcg += subRecipeRow.RequiredGold;
                endBlockIndex += subRecipeRow.RequiredBlockIndex;
            }
            // ~Validate SubRecipeId

            costActionPoint += recipeRow.RequiredActionPoint;
            costNcg += recipeRow.RequiredGold;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            // ~Validate Work

            var existHammerPointSheet =
                sheets.TryGetSheet(out CrystalHammerPointSheet hammerPointSheet);
            var hammerPointAddress =
                Addresses.GetHammerPointStateAddress(avatarAddress, recipeId);
            var hammerPointState = new HammerPointState(hammerPointAddress, recipeId);
            CrystalHammerPointSheet.Row hammerPointRow = null;
            if (existHammerPointSheet)
            {
                if (states.TryGetState(hammerPointAddress, out List serialized))
                {
                    hammerPointState =
                        new HammerPointState(hammerPointAddress, serialized);
                }

                // Validate HammerPointSheet by recipeId
                if (!hammerPointSheet.TryGetValue(recipeId, out hammerPointRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(CrystalHammerPointSheet),
                        recipeId);
                }
            }

            var isMimisbrunnrSubRecipe = subRecipeRow?.IsMimisbrunnrSubRecipe ??
                subRecipeId.HasValue && recipeRow.SubRecipeIds[2] == subRecipeId.Value;
            var petOptionSheet = states.GetSheet<PetOptionSheet>();
            if (useHammerPoint)
            {
                if (!existHammerPointSheet)
                {
                    throw new FailedLoadSheetException(typeof(CrystalHammerPointSheet));
                }

                if (isMimisbrunnrSubRecipe)
                {
                    throw new ArgumentException(
                        $"Can not super craft with mimisbrunnr recipe. Subrecipe id: {subRecipeId}");
                }

                if (hammerPointState.HammerPoint < hammerPointRow.MaxPoint)
                {
                    throw new NotEnoughHammerPointException(
                        $"Not enough hammer points. Need : {hammerPointRow.MaxPoint}, own : {hammerPointState.HammerPoint}");
                }

                states = UseAssetsBySuperCraft(
                    states,
                    context,
                    hammerPointRow,
                    hammerPointState);
            }
            else
            {
                states = UseAssetsByNormalCombination(
                    states,
                    context,
                    avatarState,
                    hammerPointState,
                    petState,
                    sheets,
                    materialItemSheet,
                    hammerPointSheet,
                    petOptionSheet,
                    recipeRow,
                    subRecipeRow,
                    requiredFungibleItems,
                    addressesHex);
            }

            // Subtract Required ActionPoint
            if (costActionPoint > 0)
            {
                if (avatarState.actionPoint < costActionPoint)
                {
                    throw new NotEnoughActionPointException(
                        $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {costActionPoint}"
                    );
                }

                avatarState.actionPoint -= costActionPoint;
            }
            // ~Subtract Required ActionPoint

            // Transfer Required NCG
            if (costNcg > 0L)
            {
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);

                states = states.TransferAsset(
                    context,
                    context.Signer,
                    feeStoreAddress,
                    states.GetGoldCurrency() * costNcg
                );
            }
            // ~Transfer Required NCG

            // Create Equipment
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipmentRow,
                context.Random.GenerateRandomGuid(),
                endBlockIndex,
                madeWithMimisbrunnrRecipe: isMimisbrunnrSubRecipe
            );

            if (!(subRecipeRow is null))
            {
                AddAndUnlockOption(
                    agentState,
                    petState,
                    equipment,
                    context.Random,
                    subRecipeRow,
                    sheets.GetSheet<EquipmentItemOptionSheet>(),
                    petOptionSheet,
                    sheets.GetSheet<SkillSheet>()
                );
                endBlockIndex = equipment.RequiredBlockIndex;

                if (useHammerPoint)
                {
                    if (!equipment.Skills.Any())
                    {
                        AddSkillOption(
                            agentState,
                            equipment,
                            context.Random,
                            subRecipeRow,
                            sheets.GetSheet<EquipmentItemOptionSheet>(),
                            sheets.GetSheet<SkillSheet>()
                        );
                    }

                    var firstFoodRow = sheets.GetSheet<ConsumableItemRecipeSheet>()
                        .First;
                    if (firstFoodRow is null)
                    {
                        throw new SheetRowNotFoundException(
                            $"{nameof(ConsumableItemRecipeSheet)}'s first row is null.", 0);
                    }

                    endBlockIndex = equipment.RequiredBlockIndex =
                        context.BlockIndex + firstFoodRow.RequiredBlockIndex;
                }
            }
            // ~Create Equipment

            // Apply block time discount
            if (!(petState is null))
            {
                var requiredBlockIndex = endBlockIndex - context.BlockIndex;
                var gameConfigState = states.GetGameConfigState();
                requiredBlockIndex = PetHelper.CalculateReducedBlockOnCraft(
                    requiredBlockIndex,
                    gameConfigState.RequiredAppraiseBlock,
                    petState,
                    petOptionSheet);
                endBlockIndex = context.BlockIndex + requiredBlockIndex;
                equipment.Update(endBlockIndex);
            }

            // Add or Update Equipment
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;
            avatarState.questList.UpdateCombinationEquipmentQuest(recipeId);
            avatarState.UpdateFromCombination(equipment);
            avatarState.UpdateQuestRewards(materialItemSheet);
            // ~Add or Update Equipment

            // Update Slot
            var mailId = context.Random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = costActionPoint,
                gold = costNcg,
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = equipment,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex, petId);
            // ~Update Slot

            // Update Pet
            if (!(petState is null))
            {
                petState.Update(endBlockIndex);
                var petStateAddress = PetState.DeriveAddress(avatarAddress, petState.PetId);
                states = states.SetState(petStateAddress, petState.Serialize());
            }
            // ~Update Pet

            // Create Mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);
            // ~Create Mail

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CombinationEquipment Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(hammerPointAddress,hammerPointState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        private IAccountStateDelta UseAssetsBySuperCraft(
            IAccountStateDelta states,
            IActionContext context,
            CrystalHammerPointSheet.Row row,
            HammerPointState hammerPointState)
        {
            var crystalBalance = states.GetBalance(context.Signer, CrystalCalculator.CRYSTAL);
            var hammerPointCost = CrystalCalculator.CRYSTAL * row.CRYSTAL;
            if (crystalBalance < hammerPointCost)
            {
                throw new NotEnoughFungibleAssetValueException($"required {hammerPointCost}, but balance is {crystalBalance}");
            }

            hammerPointState.ResetHammerPoint();
            return states.TransferAsset(
                context,
                context.Signer,
                Addresses.SuperCraft,
                hammerPointCost);
        }

        private IAccountStateDelta UseAssetsByNormalCombination(
            IAccountStateDelta states,
            IActionContext context,
            AvatarState avatarState,
            HammerPointState hammerPointState,
            PetState petState,
            Dictionary<Type, (Address, ISheet)> sheets,
            MaterialItemSheet materialItemSheet,
            CrystalHammerPointSheet hammerPointSheet,
            PetOptionSheet petOptionSheet,
            EquipmentItemRecipeSheet.Row recipeRow,
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow,
            Dictionary<int, int> requiredFungibleItems,
            string addressesHex)
        {
            // Remove Required Materials
            var inventory = avatarState.inventory;
            var crystalMaterialSheet = sheets.GetSheet<CrystalMaterialCostSheet>();
            var costCrystal = CrystalCalculator.CRYSTAL * 0;
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                var itemId = pair.Key;
                var requiredCount = pair.Value;
                if (materialItemSheet.TryGetValue(itemId, out var materialRow))
                {
                    int itemCount = inventory.TryGetItem(itemId, out Inventory.Item item)
                        ? item.count
                        : 0;
                    if (itemCount < requiredCount && payByCrystal)
                    {
                        costCrystal += CrystalCalculator.CalculateMaterialCost(
                            itemId,
                            requiredCount - itemCount,
                            crystalMaterialSheet);
                        requiredCount = itemCount;
                    }

                    if (requiredCount > 0 && !inventory.RemoveFungibleItem(materialRow.ItemId,
                            context.BlockIndex,
                            requiredCount))
                    {
                        throw new NotEnoughMaterialException(
                            $"{addressesHex}Aborted as the player has no enough material ({pair.Key} * {pair.Value})");
                    }
                }
                else
                {
                    throw new SheetRowNotFoundException(nameof(MaterialItemSheet), itemId);
                }
            }

            // ~Remove Required Materials
            if (costCrystal > 0 * CrystalCalculator.CRYSTAL)
            {
                var crystalFluctuationSheet = sheets.GetSheet<CrystalFluctuationSheet>();
                var row = crystalFluctuationSheet.Values
                    .First(r => r.Type == CrystalFluctuationSheet.ServiceType.Combination);
                var (dailyCostState, weeklyCostState, _, _) =
                    states.GetCrystalCostStates(context.BlockIndex, row.BlockInterval);

                // 1x fixed crystal cost.
                costCrystal = CrystalCalculator.CalculateCombinationCost(
                    costCrystal,
                    row: row,
                    prevWeeklyCostState: null,
                    beforePrevWeeklyCostState: null);

                // Apply pet discount if possible.
                if (!(petState is null))
                {
                    costCrystal = PetHelper.CalculateDiscountedMaterialCost(
                        costCrystal,
                        petState,
                        petOptionSheet);
                }

                // Update Daily Formula.
                dailyCostState.Count++;
                dailyCostState.CRYSTAL += costCrystal;
                // Update Weekly Formula.
                weeklyCostState.Count++;
                weeklyCostState.CRYSTAL += costCrystal;

                var crystalBalance =
                    states.GetBalance(context.Signer, CrystalCalculator.CRYSTAL);
                if (costCrystal > crystalBalance)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        $"required {costCrystal}, but balance is {crystalBalance}");
                }

                states = states
                    .SetState(dailyCostState.Address, dailyCostState.Serialize())
                    .SetState(weeklyCostState.Address, weeklyCostState.Serialize())
                    .TransferAsset(context, context.Signer, Addresses.MaterialCost, costCrystal);
            }

            int hammerPoint;
            if (subRecipeRow?.RewardHammerPoint.HasValue ?? false)
            {
                hammerPoint = subRecipeRow.RewardHammerPoint.Value;
            }
            else
            {
                var isBasicSubRecipe = !subRecipeId.HasValue ||
                                       recipeRow.SubRecipeIds[0] == subRecipeId.Value;
                hammerPoint = isBasicSubRecipe
                    ? BasicSubRecipeHammerPoint
                    : SpecialSubRecipeHammerPoint;
            }

            hammerPointState.AddHammerPoint(hammerPoint, hammerPointSheet);
            return states;
        }

        public static void AddAndUnlockOption(
            AgentState agentState,
            PetState petState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            PetOptionSheet petOptionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                var ratio = optionInfo.Ratio;

                // Apply pet bonus if possible
                if (!(petState is null))
                {
                    ratio = PetHelper.GetBonusOptionProbability(
                        ratio,
                        petState,
                        petOptionSheet);
                }

                if (value > ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
                else
                {
                    var skill = CombinationEquipment.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                        equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                        equipment.optionCountFromCombination++;
                        agentState.unlockedOptions.Add(optionRow.Id);
                    }
                }
            }
        }

        public static Skill GetSkill(
            EquipmentItemOptionSheet.Row row,
            SkillSheet skillSheet,
            IRandom random)
        {
            var skillRow = skillSheet.OrderedList.FirstOrDefault(r => r.Id == row.SkillId);
            if (skillRow == null)
            {
                return null;
            }

            var dmg = random.Next(row.SkillDamageMin, row.SkillDamageMax + 1);
            var chance = random.Next(row.SkillChanceMin, row.SkillChanceMax + 1);

            var hasStatDamageRatio = row.StatDamageRatioMin != default && row.StatDamageRatioMax != default;
            var statDamageRatio = hasStatDamageRatio ?
                random.Next(row.StatDamageRatioMin, row.StatDamageRatioMax + 1) : default;
            var refStatType = hasStatDamageRatio ? row.ReferencedStatType : StatType.NONE;

            var skill = SkillFactory.Get(skillRow, dmg, chance, statDamageRatio, refStatType);
            return skill;
        }

        public static void AddSkillOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options)
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var skill = GetSkill(optionRow, skillSheet, random);
                if (!(skill is null))
                {
                    equipment.Skills.Add(skill);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
            }
        }
    }
}

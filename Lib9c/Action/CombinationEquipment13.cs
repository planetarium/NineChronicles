using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;

using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1264
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100282ObsoleteIndex)]
    [ActionType("combination_equipment13")]
    public class CombinationEquipment13 : GameAction, ICombinationEquipmentV3
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
        public const int BasicSubRecipeHammerPoint = 1;
        public const int SpecialSubRecipeHammerPoint = 2;

        Address ICombinationEquipmentV3.AvatarAddress => avatarAddress;
        int ICombinationEquipmentV3.RecipeId => recipeId;
        int ICombinationEquipmentV3.SlotIndex => slotIndex;
        int? ICombinationEquipmentV3.SubRecipeId => subRecipeId;
        bool ICombinationEquipmentV3.PayByCrystal => payByCrystal;
        bool ICombinationEquipmentV3.UseHammerPoint => useHammerPoint;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
                [SubRecipeIdKey] = subRecipeId.Serialize(),
                [PayByCrystalKey] = payByCrystal.Serialize(),
                [UseHammerPointKey] = useHammerPoint.Serialize(),
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
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
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

            CheckObsolete(ActionObsoleteConfig.V100282ObsoleteIndex, context);

            if (recipeId != 1)
            {
                var unlockedRecipeIdsAddress = avatarAddress.Derive("recipe_ids");
                if (!states.TryGetState(unlockedRecipeIdsAddress, out List rawIds))
                {
                    throw new FailedLoadStateException("can't find UnlockedRecipeList.");
                }

                List<int> unlockedIds = rawIds.ToList(StateExtensions.ToInteger);
                if (!unlockedIds.Contains(recipeId))
                {
                    throw new InvalidRecipeIdException($"unlock {recipeId} first.");
                }
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
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

            // Validate Recipe Unlocked.
            if (!avatarState.worldInformation.IsStageCleared(recipeRow.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    recipeRow.UnlockStage,
                    current);
            }
            // ~Validate Recipe Unlocked

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

            if (useHammerPoint)
            {
                if (!existHammerPointSheet)
                {
                    throw new FailedLoadSheetException(typeof(CrystalHammerPointSheet));
                }

                if (recipeRow.IsMimisBrunnrSubRecipe(subRecipeId))
                {
                    throw new ArgumentException(
                        $"Can not super craft with mimisbrunnr recipe. Subrecipe id: {subRecipeId}");
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
                    sheets,
                    materialItemSheet,
                    hammerPointSheet,
                    recipeRow,
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
                madeWithMimisbrunnrRecipe: recipeRow.IsMimisBrunnrSubRecipe(subRecipeId));

            if (!(subRecipeRow is null))
            {
                AddAndUnlockOption(
                    agentState,
                    equipment,
                    context.Random,
                    subRecipeRow,
                    sheets.GetSheet<EquipmentItemOptionSheet>(),
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
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);
            // ~Update Slot

            // Create Mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);
            // ~Create Mail

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
                context.Signer,
                Addresses.SuperCraft,
                hammerPointCost);
        }

        private IAccountStateDelta UseAssetsByNormalCombination(
            IAccountStateDelta states,
            IActionContext context,
            AvatarState avatarState,
            HammerPointState hammerPointState,
            Dictionary<Type, (Address, ISheet)> sheets,
            MaterialItemSheet materialItemSheet,
            CrystalHammerPointSheet hammerPointSheet,
            EquipmentItemRecipeSheet.Row recipeRow,
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
                        .TransferAsset(context.Signer, Addresses.MaterialCost, costCrystal);
                }

                var isBasicSubRecipe = !subRecipeId.HasValue ||
                                       recipeRow.SubRecipeIds[0] == subRecipeId.Value;

                hammerPointState.AddHammerPoint(
                    isBasicSubRecipe ? BasicSubRecipeHammerPoint : SpecialSubRecipeHammerPoint,
                    hammerPointSheet);
                return states;
        }

        public static void AddAndUnlockOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
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
                if (value > optionInfo.Ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
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

                Skill skill;
                try
                {
                    var skillRow = skillSheet.OrderedList.First(r => r.Id == optionRow.SkillId);
                    var dmg = random.Next(optionRow.SkillDamageMin, optionRow.SkillDamageMax + 1);
                    var chance = random.Next(optionRow.SkillChanceMin, optionRow.SkillChanceMax + 1);
                    skill = SkillFactory.Get(skillRow, dmg, chance);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

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

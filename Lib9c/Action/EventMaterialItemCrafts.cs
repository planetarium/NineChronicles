using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventMaterialItemCrafts : GameAction
    {
        private const string ActionTypeText = "event_material_item_crafts";
        public Address AvatarAddress;
        public int EventScheduleId;
        public int EventMaterialItemRecipeId;
        public Dictionary<int, int> MaterialsToUse;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var serialized = new Dictionary(MaterialsToUse
                    .OrderBy(pair => pair.Key)
                    .Select(pair =>
                        new KeyValuePair<IKey, IValue>(
                            (IKey)pair.Key.Serialize(), pair.Value.Serialize()
                        )
                    ));
                var list = List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(EventScheduleId.Serialize())
                    .Add(EventMaterialItemRecipeId.Serialize())
                    .Add(serialized);

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            if (!plainValue.TryGetValue("l", out var serialized))
            {
                throw new ArgumentException("plainValue must contain 'l'");
            }

            if (!(serialized is List list))
            {
                throw new ArgumentException("'l' must be a bencodex list");
            }

            if (list.Count < 4)
            {
                throw new ArgumentException("'l' must contain at least 4 items");
            }

            AvatarAddress = list[0].ToAddress();
            EventScheduleId = list[1].ToInteger();
            EventMaterialItemRecipeId = list[2].ToInteger();
            var deserialized = ((Dictionary)list[3]).ToDictionary(pair =>
                pair.Key.ToInteger(), pair => pair.Value.ToInteger());
            MaterialsToUse = deserialized;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Execute() start",
                ActionTypeText,
                addressesHex);

            var sw = new Stopwatch();

            #region Get AvatarState
            // Get AvatarState
            sw.Start();
            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }
            sw.Stop();

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] TryGetAvatarStateV2: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get AvatarState
            #endregion

            #region Get sheets
            // Get sheets
            sw.Restart();
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(EventScheduleSheet),
                    typeof(EventMaterialItemRecipeSheet),
                });
            sw.Stop();

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Get sheets: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get sheets
            #endregion

            #region Validate Requirements
            // Validate Requirements
            sw.Restart();
            avatarState.worldInformation.ValidateFromAction(
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction,
                ActionTypeText,
                addressesHex);
            sw.Stop();

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate requirements: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate Requirements
            #endregion

            #region Validate fields
            // Validate fields
            sw.Restart();
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            scheduleSheet.ValidateFromActionForRecipe(
                context.BlockIndex,
                EventScheduleId,
                EventMaterialItemRecipeId,
                ActionTypeText,
                addressesHex);

            var recipeSheet = sheets.GetSheet<EventMaterialItemRecipeSheet>();
            var recipeRow = recipeSheet.ValidateFromAction(
                EventMaterialItemRecipeId,
                ActionTypeText,
                addressesHex);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate fields
            #endregion

            #region Validate Work
            // Validate Work
            sw.Restart();
            var costActionPoint = 0;

            // Validate Recipe ResultMaterialItemId
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            if (!materialItemSheet.TryGetValue(
                    recipeRow.ResultMaterialItemId,
                    out var resulMaterialRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(materialItemSheet),
                    recipeRow.ResultMaterialItemId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            recipeRow.ValidateFromAction(
                materialItemSheet,
                MaterialsToUse,
                ActionTypeText,
                addressesHex);
            // ~Validate Recipe Material

            costActionPoint += recipeRow.RequiredActionPoint;

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate work: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate Work
            #endregion

            #region Remove Required Materials
            // Remove Required Materials
            var inventory = avatarState.inventory;
#pragma warning disable LAA1002
            foreach (var pair in MaterialsToUse)
#pragma warning restore LAA1002
            {
                if (!materialItemSheet.TryGetValue(pair.Key, out var materialRow) ||
                    !inventory.RemoveFungibleItem(materialRow.ItemId, context.BlockIndex, pair.Value))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({pair.Key} * {pair.Value})");
                }
            }
            // ~Remove Required Materials
            #endregion

            #region Subtract Required ActionPoint
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
            #endregion

            #region Create Material
            // Create Material
            var materialResult = ItemFactory.CreateMaterial(resulMaterialRow);
            avatarState.inventory.AddItem(materialResult);
            // ~Create Material
            #endregion

            #region Create Mail
            // Create Mail
            var mail = new MaterialCraftMail(
                context.BlockIndex,
                Id,
                context.BlockIndex,
                1,
                materialResult);
            avatarState.Update(mail);
            // ~Create Mail
            #endregion

            #region Set states
            // Set states
            sw.Restart();
            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }
            else
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize());
            }
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Set states: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Set states
            #endregion

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Total elapsed: {Elapsed}",
                ActionTypeText,
                addressesHex,
                DateTimeOffset.UtcNow - started);

            return states;
        }

    }
}

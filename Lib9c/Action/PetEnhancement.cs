using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("pet_enhancement")]
    public class PetEnhancement : GameAction
    {
        public Address AvatarAddress;
        public int PetId;
        public int TargetLevel;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = PetId.Serialize(),
                ["t"] = TargetLevel.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            PetId = plainValue["p"].ToInteger();
            TargetLevel = plainValue["t"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out _, out _))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(PetSheet),
                    typeof(PetCostSheet),
                });

            if (TargetLevel < 1)
            {
                throw new InvalidActionFieldException(
                    $"{AvatarAddress}TargetLevel must be greater than 0. " +
                    $"current TargetLevel : {TargetLevel}");
            }

            var petStateAddress = PetState.DeriveAddress(AvatarAddress, PetId);
            var petState = states.TryGetState(petStateAddress, out List rawState)
                ? new PetState(rawState)
                : new PetState(PetId);

            if (petState.Level >= TargetLevel)
            {
                throw new InvalidActionFieldException(
                    $"TargetLevel({TargetLevel}) must be higher than now pet level({petState.Level}).");
            }

            var petSheet = sheets.GetSheet<PetSheet>();
            if (!petSheet.TryGetValue(petState.PetId, out var petRow))
            {
                throw new SheetRowNotFoundException(nameof(petSheet), PetId);
            }

            var costSheet = sheets.GetSheet<PetCostSheet>();
            if (!costSheet.TryGetValue(petState.PetId, out var costRow))
            {
                throw new SheetRowNotFoundException(nameof(costSheet), petState.PetId);
            }

            if (!costRow.TryGetCost(TargetLevel, out _))
            {
                // cost not found with level
                throw new PetCostNotFoundException(
                    $"[{nameof(PetEnhancement)}] Can not find cost by TargetLevel({TargetLevel}).");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var soulStoneCurrency = Currency.Legacy(petRow.SoulStoneTicker, 0, minters: null);
            var (ncgQuantity, soulStoneQuantity) = PetHelper.CalculateEnhancementCost(
                costSheet,
                PetId,
                petState.Level,
                TargetLevel);
            while (petState.Level < TargetLevel)
            {
                petState.LevelUp();
            }

            states = states.SetState(petStateAddress, petState.Serialize());

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);
            var ncgCost = ncgQuantity * ncgCurrency;
            if (ncgQuantity > 0)
            {
                states = states.TransferAsset(context.Signer, feeStoreAddress, ncgCost);
            }

            var soulStoneCost = soulStoneQuantity * soulStoneCurrency;
            if (soulStoneQuantity > 0)
            {
                states = states.TransferAsset(AvatarAddress, feeStoreAddress, soulStoneCost);
            }

            return states;
        }
    }
}

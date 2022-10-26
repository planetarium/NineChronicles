using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("runeEnhancement")]
    public class RuneEnhancement : GameAction
    {
        public Address AvatarAddress;
        public int RuneId;
        public bool Once;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = RuneId.Serialize(),
                ["o"] = Once.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            RuneId = plainValue["r"].ToInteger();
            Once = plainValue["o"].ToBoolean();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                });

            RuneState runeState;
            var runeStateAddress = RuneState.DeriveAddress(AvatarAddress, RuneId);
            if (states.TryGetState(runeStateAddress, out List rawState))
            {
                runeState = new RuneState(rawState);
            }
            else
            {
                runeState = new RuneState(RuneId);
            }

            var costSheet = sheets.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeState.RuneId, out var costRow))
            {
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var targetLevel = runeState.Level + 1;
            if (!costRow.TryGetCost(targetLevel, out var cost))
            {
                throw new RuneCostDataNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var runeSheet = sheets.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(cost.RuneStoneId, out var runeRow))
            {
                throw new RuneNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);
            var ncgBalance = states.GetBalance(context.Signer, ncgCurrency);
            var crystalBalance = states.GetBalance(context.Signer, crystalCurrency);
            var runeBalance = states.GetBalance(AvatarAddress, runeCurrency);
            if (RuneHelper.TryEnhancement(ncgBalance, crystalBalance, runeBalance,
                    ncgCurrency, crystalCurrency, runeCurrency,
                    cost, context.Random, Once, out var tryCount))
            {
                runeState.LevelUp();
            }

            var ncgCost = cost.NcgQuantity * tryCount * ncgCurrency;
            var crystalCost = cost.CrystalQuantity * tryCount * crystalCurrency;
            var runeCost = cost.RuneStoneQuantity * tryCount * runeCurrency;
            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);
            return states.SetState(runeStateAddress, runeState.Serialize())
                .TransferAsset(context.Signer, feeStoreAddress, ncgCost)
                .TransferAsset(context.Signer, feeStoreAddress, crystalCost)
                .TransferAsset(AvatarAddress, feeStoreAddress, runeCost);
        }
    }
}

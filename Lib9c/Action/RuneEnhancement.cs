using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
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
    [ActionType("runeEnhancement2")]
    public class RuneEnhancement : GameAction, IRuneEnhancementV1
    {
        public Address AvatarAddress;
        public int RuneId;
        public int TryCount = 1;

        Address IRuneEnhancementV1.AvatarAddress => AvatarAddress;
        int IRuneEnhancementV1.RuneId => RuneId;
        int IRuneEnhancementV1.TryCount => TryCount;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = RuneId.Serialize(),
                ["t"] = TryCount.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            RuneId = plainValue["r"].ToInteger();
            TryCount = plainValue["t"].ToInteger();
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
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                });

            if (TryCount < 1)
            {
                throw new TryCountIsZeroException(
                    $"{AvatarAddress}TryCount must be greater than 0. " +
                    $"current TryCount : {TryCount}");
            }

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
            if (!runeSheet.TryGetValue(runeState.RuneId, out var runeRow))
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
                    cost, context.Random, TryCount, out var tryCount))
            {
                runeState.LevelUp();
                states = states.SetState(runeStateAddress, runeState.Serialize());
            }

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);

            var ncgCost = cost.NcgQuantity * tryCount * ncgCurrency;
            if (cost.NcgQuantity > 0)
            {
                states = states.TransferAsset(context.Signer, feeStoreAddress, ncgCost);
            }

            var crystalCost = cost.CrystalQuantity * tryCount * crystalCurrency;
            if (cost.CrystalQuantity > 0)
            {
                states = states.TransferAsset(context.Signer, feeStoreAddress, crystalCost);
            }

            var runeCost = cost.RuneStoneQuantity * tryCount * runeCurrency;
            if (cost.RuneStoneQuantity > 0)
            {
                states = states.TransferAsset(AvatarAddress, feeStoreAddress, runeCost);
            }

            return states;
        }
    }
}

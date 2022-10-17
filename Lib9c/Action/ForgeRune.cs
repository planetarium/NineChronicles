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
    [ActionType("forgeRune")]
    public class ForgeRune : GameAction
    {
        public Address AvatarAddress;
        public int RuneId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = RuneId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            RuneId = plainValue["r"].ToInteger();
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
                    $"[{nameof(ForgeRune)}] my avatar address : {AvatarAddress}");
            }

            if (!costRow.TryGetCost(runeState.Level, out var cost))
            {
                throw new RuneCostDataNotFoundException(
                    $"[{nameof(ForgeRune)}] my avatar address : {AvatarAddress}");
            }

            var runeSheet = sheets.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(cost.RuneStoneId, out var runeRow))
            {
                throw new RuneNotFoundException(
                    $"[{nameof(ForgeRune)}] my avatar address : {AvatarAddress}");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);
            var ncgBalance = states.GetBalance(context.Signer, ncgCurrency);
            var crystalBalance = states.GetBalance(context.Signer, crystalCurrency);
            var runeBalance = states.GetBalance(AvatarAddress, runeCurrency);
            if (TryForge(ncgBalance, crystalBalance, runeBalance,
                    ncgCurrency, crystalCurrency, runeCurrency,
                    cost, context.Random, out var tryCount))
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

        private bool TryForge(
            FungibleAssetValue ncg,
            FungibleAssetValue crystal,
            FungibleAssetValue rune,
            Currency ncgCurrency,
            Currency crystalCurrency,
            Currency runeCurrency,
            RuneCostSheet.RuneCostData cost,
            IRandom random,
            out int tryCount)
        {
            tryCount = 0;
            var value = cost.LevelUpSuccessRate + 1;
            while (value > cost.LevelUpSuccessRate)
            {
                tryCount++;
                var ncgCost = cost.NcgQuantity * tryCount * ncgCurrency;
                var crystalCost = cost.CrystalQuantity * tryCount * crystalCurrency;
                var runeCost = cost.RuneStoneQuantity * tryCount * runeCurrency;
                if (ncg < ncgCost || crystal < crystalCost || rune < runeCost)
                {
                    tryCount--;
                    if (tryCount == 0)
                    {
                        throw new NotEnoughFungibleAssetValueException($"{nameof(ForgeRune)}" +
                            $"[ncg:{ncg} < {ncgCost}] [crystal:{crystal} < {crystalCost}] [rune:{rune} < {runeCost}]");
                    }
                    return false;
                }
                value = random.Next(1, GameConfig.MaximumProbability + 1);
            }

            return true;
        }
    }
}

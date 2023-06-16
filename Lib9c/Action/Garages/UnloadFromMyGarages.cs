#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.State;
using Nekoyume.Exceptions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("unload_from_my_garages")]
    public class UnloadFromMyGarages : GameAction, IUnloadFromMyGarages, IAction
    {
        public IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues { get; private set; }

        /// <summary>
        /// This address does not need to consider its owner.
        /// If the avatar state is v1, there is no separate inventory,
        /// so it should be execute another action first to migrate the avatar state to v2.
        /// And then, the inventory address will be set.
        /// </summary>
        public Address? InventoryAddr { get; private set; }

        public IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts { get; private set; }

        public string? Memo { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                {
                    "l",
                    new List(
                        FungibleAssetValues is null
                            ? (IValue)Null.Value
                            : new List(FungibleAssetValues.Select(tuple => new List(
                                tuple.balanceAddr.Serialize(),
                                tuple.value.Serialize()))),
                        InventoryAddr is null
                            ? Null.Value
                            : InventoryAddr.Serialize(),
                        FungibleIdAndCounts is null
                            ? (IValue)Null.Value
                            : new List(FungibleIdAndCounts.Select(tuple => new List(
                                tuple.fungibleId.Serialize(),
                                (Integer)tuple.count))),
                        Memo is null
                            ? (IValue)Null.Value
                            : (Text)Memo)
                }
            }.ToImmutableDictionary();

        public UnloadFromMyGarages(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            (
                FungibleAssetValues,
                InventoryAddr,
                FungibleIdAndCounts,
                Memo
            ) = GarageUtils.MergeAndSort(
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                memo);
        }

        public UnloadFromMyGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            (
                FungibleAssetValues,
                InventoryAddr,
                FungibleIdAndCounts,
                Memo
            ) = GarageUtils.Deserialize(plainValue["l"]);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context);
            ValidateFields(addressesHex);
            states = TransferFungibleAssetValues(
                context.Signer,
                states);
            return TransferFungibleItems(
                context.Signer,
                states);
        }

        private void ValidateFields(string addressesHex)
        {
            if (FungibleAssetValues is null &&
                FungibleIdAndCounts is null)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] Either FungibleAssetValues or FungibleIdAndCounts " +
                    "must be set.");
            }

            if (FungibleAssetValues != null)
            {
                foreach (var (_, value) in FungibleAssetValues)
                {
                    if (value.Sign < 0)
                    {
                        throw new InvalidActionFieldException(
                            $"[{addressesHex}] FungibleAssetValue.Sign must be positive.");
                    }
                }
            }

            if (FungibleIdAndCounts is null)
            {
                return;
            }

            if (!InventoryAddr.HasValue)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] {nameof(InventoryAddr)} is required when " +
                    $"{nameof(FungibleIdAndCounts)} is set.");
            }

            foreach (var (fungibleId, count) in FungibleIdAndCounts)
            {
                if (count < 0)
                {
                    throw new InvalidActionFieldException(
                        $"[{addressesHex}] Count of fungible id must be positive." +
                        $" {fungibleId}, {count}");
                }
            }
        }

        private IAccountStateDelta TransferFungibleAssetValues(
            Address signer,
            IAccountStateDelta states)
        {
            if (FungibleAssetValues is null)
            {
                return states;
            }

            var garageBalanceAddress =
                Addresses.GetGarageBalanceAddress(signer);
            foreach (var (balanceAddr, value) in FungibleAssetValues)
            {
                states = states.TransferAsset(garageBalanceAddress, balanceAddr, value);
            }

            return states;
        }

        private IAccountStateDelta TransferFungibleItems(
            Address signer,
            IAccountStateDelta states)
        {
            if (InventoryAddr is null ||
                FungibleIdAndCounts is null)
            {
                return states;
            }

            var inventory = states.GetInventory(InventoryAddr.Value);
            var fungibleItemTuples = GarageUtils.WithGarageTuples(
                signer,
                states,
                FungibleIdAndCounts);
            foreach (var (_, count, garageAddr, garage) in fungibleItemTuples)
            {
                garage.Unload(count);
                inventory.AddTradableFungibleItem(
                    (ITradableFungibleItem)garage.Item,
                    count);
                states = states.SetState(garageAddr, garage.Serialize());
            }

            return states.SetState(InventoryAddr.Value, inventory.Serialize());
        }
    }
}

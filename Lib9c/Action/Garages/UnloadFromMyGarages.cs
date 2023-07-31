#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("unload_from_my_garages")]
    public class UnloadFromMyGarages : GameAction, IUnloadFromMyGaragesV1, IAction
    {
        /// <summary>
        /// If the avatar state is v1, there is no separate inventory,
        /// so it should be execute another action first to migrate the avatar state to v2.
        /// And then, the inventory address will be set.
        /// </summary>
        public Address RecipientAvatarAddr { get; private set; }

        public IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues { get; private set; }

        public IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts { get; private set; }

        public string? Memo { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                {
                    "l",
                    new List(
                        RecipientAvatarAddr.Serialize(),
                        FungibleAssetValues is null
                            ? (IValue)Null.Value
                            : new List(FungibleAssetValues.Select(tuple => new List(
                                tuple.balanceAddr.Serialize(),
                                tuple.value.Serialize()))),
                        FungibleIdAndCounts is null
                            ? (IValue)Null.Value
                            : new List(FungibleIdAndCounts.Select(tuple => new List(
                                tuple.fungibleId.Serialize(),
                                (Integer)tuple.count))),
                        string.IsNullOrEmpty(Memo)
                            ? (IValue)Null.Value
                            : (Text)Memo)
                }
            }.ToImmutableDictionary();

        public UnloadFromMyGarages(
            Address recipientAvatarAddr,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            RecipientAvatarAddr = recipientAvatarAddr;
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCounts);
            Memo = memo;
        }

        public UnloadFromMyGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = plainValue["l"];
            if (serialized is null || serialized is Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            if (!(serialized is List list))
            {
                throw new ArgumentException(
                    $"The type of {nameof(serialized)} must be bencodex list.");
            }

            RecipientAvatarAddr = list[0].ToAddress();
            var fungibleAssetValues = list[1].Kind == ValueKind.Null
                ? null
                : ((List)list[1]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToAddress(),
                        l2[1].ToFungibleAssetValue());
                });
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            var fungibleIdAndCounts = list[2].Kind == ValueKind.Null
                ? null
                : ((List)list[2]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToItemId(),
                        (int)((Integer)l2[1]).Value);
                });
            FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCounts);
            Memo = list[3].Kind == ValueKind.Null
                ? null
                : (string)(Text)list[3];
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var state = context.PreviousState;
            if (context.Rehearsal)
            {
                return state;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context);
            ValidateFields(addressesHex);
            state = TransferFungibleAssetValues(context, state);
            state = TransferFungibleItems(context.Signer, state);
            return SendMail(context.BlockIndex, context.Random, state);
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
            IActionContext context,
            IAccountStateDelta states)
        {
            if (FungibleAssetValues is null)
            {
                return states;
            }

            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(context.Signer);
            foreach (var (balanceAddr, value) in FungibleAssetValues)
            {
                states = states.TransferAsset(context, garageBalanceAddress, balanceAddr, value);
            }

            return states;
        }

        private IAccountStateDelta TransferFungibleItems(
            Address signer,
            IAccountStateDelta states)
        {
            if (FungibleIdAndCounts is null)
            {
                return states;
            }

            var inventoryAddr = RecipientAvatarAddr.Derive(SerializeKeys.LegacyInventoryKey);
            var inventory = states.GetInventory(inventoryAddr);
            var fungibleItemTuples = GarageUtils.WithGarageTuples(
                signer,
                states,
                FungibleIdAndCounts);
            foreach (var (_, count, garageAddr, garage) in fungibleItemTuples)
            {
                garage.Unload(count);
                inventory.AddFungibleItem((ItemBase)garage.Item, count);
                states = states.SetState(garageAddr, garage.Serialize());
            }

            return states.SetState(inventoryAddr, inventory.Serialize());
        }

        private IAccountStateDelta SendMail(
            long blockIndex,
            IRandom random,
            IAccountStateDelta states)
        {
            var avatarValue = states.GetState(RecipientAvatarAddr);
            if (!(avatarValue is Dictionary avatarDict))
            {
                throw new FailedLoadStateException(RecipientAvatarAddr, typeof(AvatarState));
            }

            // NOTE:
            // This action supports the avatar state v2 only.
            // So, we just check the mail box with a newer key.
            if (!avatarDict.ContainsKey(SerializeKeys.MailBoxKey))
            {
                throw new KeyNotFoundException(
                    $"Dictionary key is not found: {SerializeKeys.MailBoxKey}");
            }

            var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey]);
            mailBox.Add(new UnloadFromMyGaragesRecipientMail(
                blockIndex,
                random.GenerateRandomGuid(),
                blockIndex,
                FungibleAssetValues,
                FungibleIdAndCounts,
                Memo));
            mailBox.CleanUp();
            avatarDict = avatarDict.SetItem(SerializeKeys.MailBoxKey, mailBox.Serialize());
            return states.SetState(RecipientAvatarAddr, avatarDict);
        }
    }
}

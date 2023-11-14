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
    [ActionType("bulk_unload_from_garages")]
    public class BulkUnloadFromGarages : GameAction, IBulkUnloadFromGaragesV1, IAction
    {
        public IReadOnlyList<(
                Address recipientAvatarAddress,
                IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo)>
            UnloadData { get; private set; }

        public BulkUnloadFromGarages()
        {
            UnloadData = new List<(
                Address recipientAvatarAddress,
                IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
                fungibleIdAndCounts, string? memo)>();
        }

        public BulkUnloadFromGarages(
            IReadOnlyList<(
                Address recipientAvatarAddress,
                IEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo)> unloadData)
        {
            UnloadData = unloadData.Select(data => (
                data.recipientAvatarAddress,
                GarageUtils.MergeAndSort(data.fungibleAssetValues),
                GarageUtils.MergeAndSort(data.fungibleIdAndCounts),
                data.memo)).ToImmutableList();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                {
                    "l",
                    new List(UnloadData.Select(data =>
                        new List(data.recipientAvatarAddress.Serialize(),
                            SerializeFungibleAssetValues(data.fungibleAssetValues),
                            SerializeFungibleIdAndCounts(data.fungibleIdAndCounts),
                            string.IsNullOrEmpty(data.memo) ? Null.Value : (Text)data.memo)))
                }
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = plainValue["l"];
            if (serialized is null or Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            if (serialized is not List list)
            {
                throw new ArgumentException(
                    $"The type of {nameof(serialized)} must be bencodex list.");
            }

            UnloadData = list.Select(rawValue =>
            {
                var value = rawValue as List
                            ?? throw new ArgumentException(
                                $"The type of {nameof(rawValue)} must be bencodex list.");
                var recipientAvatarAddress = value[0].ToAddress();
                var fungibleAssetValues =
                    GarageUtils.MergeAndSort(
                        (value[1] as List)?.Select(raw =>
                        {
                            var fungibleAssetValue = (List)raw;
                            return (fungibleAssetValue[0].ToAddress(),
                                fungibleAssetValue[1].ToFungibleAssetValue());
                        }));
                var fungibleIdAndCounts =
                    GarageUtils.MergeAndSort(
                        (value[2] as List)?.Select(raw =>
                        {
                            var fungibleIdAndCount = (List)raw;
                            return (
                                fungibleIdAndCount[0].ToItemId(),
                                (int)((Integer)fungibleIdAndCount[1]).Value);
                        }));
                var memo = value[3].Kind == ValueKind.Null
                    ? null
                    : (string)(Text)value[3];

                return (recipientAvatarAddress, fungibleAssetValues, fungibleIdAndCounts, memo);
            }).ToList();
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            var states = context.PreviousState;
            if (context.Rehearsal) return states;

            // validate
            var addressesHex = GetSignerAndOtherAddressesHex(context);
            foreach (var unloadData in UnloadData)
            {
                if (unloadData.fungibleAssetValues is null &&
                    unloadData.fungibleIdAndCounts is null)
                {
                    throw new InvalidActionFieldException(
                        $"[{addressesHex}] Either FungibleAssetValues or FungibleIdAndCounts must be set.");
                }

                if (unloadData.fungibleAssetValues?.Any(fav => fav.value.Sign <= 0) ?? false)
                {
                    throw new InvalidActionFieldException(
                        $"[{addressesHex}] FungibleAssetValue.Sign must be positive");
                }

                if (unloadData.fungibleIdAndCounts?.Any(tuple => tuple.count <= 0) ?? false)
                {
                    var invalid = unloadData.fungibleIdAndCounts.First(tuple => tuple.count < 0);
                    throw new InvalidActionFieldException(
                        $"[{addressesHex}] Count of fungible id must be positive. {invalid.fungibleId}, {invalid.count}");
                }
            }

            // Execution
            foreach (var (
                         recipientAvatarAddress,
                         fungibleAssetValues,
                         fungibleIdAndCounts,
                         memo)
                     in UnloadData)
            {
                if (fungibleAssetValues is not null)
                    states = TransferFungibleAssetValues(context, states, fungibleAssetValues);
                if (fungibleIdAndCounts is not null)
                    states = TransferFungibleItems(states, context.Signer, recipientAvatarAddress,
                        fungibleIdAndCounts);
            }

            // Mailing
            var random = context.GetRandom();
            states = BulkSendMail(context.BlockIndex, random, states);

            return states;
        }

        private IAccount TransferFungibleAssetValues(
            IActionContext context,
            IAccount states,
            IEnumerable<(Address balanceAddress, FungibleAssetValue value)> fungibleAssetValues)
        {
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(context.Signer);
            foreach (var (balanceAddress, value) in fungibleAssetValues)
            {
                states = states.TransferAsset(context, garageBalanceAddress, balanceAddress, value);
            }

            return states;
        }

        private IAccount TransferFungibleItems(
            IAccount states,
            Address signer,
            Address recipientAvatarAddress,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts)
        {
            var inventoryAddress = recipientAvatarAddress.Derive(SerializeKeys.LegacyInventoryKey);
            var inventory = states.GetInventory(inventoryAddress);

            var fungibleItemTuples = GarageUtils.WithGarageTuples(
                signer,
                states,
                fungibleIdAndCounts);
            foreach (var (_, count, garageAddress, garage) in fungibleItemTuples)
            {
                garage.Unload(count);
                inventory.AddFungibleItem((ItemBase)garage.Item, count);
                states = states.SetState(garageAddress, garage.Serialize());
            }

            return states.SetState(inventoryAddress, inventory.Serialize());
        }

        private IAccount BulkSendMail(
            long blockIndex,
            IRandom random,
            IAccount states)
        {
            foreach (var tuple in UnloadData)
            {
                var (
                    recipientAvatarAddress,
                    fungibleAssetValues,
                    fungibleIdAndCounts,
                    memo) = tuple;
                var avatarValue = states.GetState(recipientAvatarAddress);
                if (!(avatarValue is Dictionary avatarDict))
                {
                    throw new FailedLoadStateException(recipientAvatarAddress, typeof(AvatarState));
                }

                if (!avatarDict.ContainsKey(SerializeKeys.MailBoxKey))
                {
                    throw new KeyNotFoundException(
                        $"Dictionary key is not found: {SerializeKeys.MailBoxKey}");
                }

                var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey])
                {
                    new UnloadFromMyGaragesRecipientMail(
                        blockIndex,
                        random.GenerateRandomGuid(),
                        blockIndex,
                        fungibleAssetValues,
                        fungibleIdAndCounts,
                        memo)
                };
                mailBox.CleanUp();
                avatarDict = avatarDict.SetItem(SerializeKeys.MailBoxKey, mailBox.Serialize());

                return states.SetState(recipientAvatarAddress, avatarDict);
            }

            return states;
        }

        private static IValue SerializeFungibleAssetValues(
            IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues)
        {
            if (fungibleAssetValues is null) return Null.Value;

            return new List(
                fungibleAssetValues.Select(tuple => new List(
                    tuple.balanceAddress.Serialize(),
                    tuple.value.Serialize())));
        }

        private static IValue SerializeFungibleIdAndCounts(
            IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            if (fungibleIdAndCounts is null) return Null.Value;

            return new List(
                fungibleIdAndCounts.Select(tuple => new List(
                    tuple.fungibleId.Serialize(),
                    (Integer)tuple.count)));
        }
    }
}

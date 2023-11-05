#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("unload_from_garages")]
    public class UnloadFromGarages : GameAction, IUnloadFromGaragesV1, IAction
    {
        public IReadOnlyList<(
                Address recipientAvatarAddress,
                IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo)>
            UnloadData { get; private set; }

        public UnloadFromGarages()
        {
            UnloadData = new List<(
                Address recipientAvatarAddress,
                IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
                fungibleIdAndCounts, string? memo)>();
        }

        public UnloadFromGarages(
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

            throw new System.NotImplementedException();
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

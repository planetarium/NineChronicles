#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Libplanet.State;
using Nekoyume.Exceptions;
using Nekoyume.Model.Garages;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    public static class GarageUtils
    {
        public static (
            IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
            Deserialize(IValue? serialized)
        {
            if (serialized is null || serialized is Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            if (!(serialized is List list))
            {
                throw new ArgumentException(
                    $"The type of {nameof(serialized)} must be bencodex list.");
            }

            var fungibleAssetValues = list[0].Kind == ValueKind.Null
                ? null
                : ((List)list[0]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToAddress(),
                        l2[1].ToFungibleAssetValue());
                });
            var inventoryAddr = list[1].Kind == ValueKind.Null
                ? (Address?)null
                : list[1].ToAddress();
            var fungibleIdAndCounts = list[2].Kind == ValueKind.Null
                ? null
                : ((List)list[2]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToItemId(),
                        (int)((Integer)l2[1]).Value);
                });
            var memo = list[3].Kind == ValueKind.Null
                ? null
                : (string)(Text)list[3];
            return MergeAndSort(
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                memo);
        }

        public static (
            IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
            MergeAndSort(
                IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
                Address? inventoryAddr,
                IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo)
        {
            (
                IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
                fungibleAssetValues,
                Address? inventoryAddr,
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo)
                result = (null, null, null, null);
            var favArr = fungibleAssetValues as (Address balanceAddr, FungibleAssetValue value)[] ??
                         fungibleAssetValues?.ToArray();
            if (favArr is null ||
                favArr.Length == 0)
            {
                result.fungibleAssetValues = null;
            }
            else
            {
                var dict = new Dictionary<Address, Dictionary<Currency, FungibleAssetValue>>();
                foreach (var (balanceAddr, value) in favArr)
                {
                    if (!dict.ContainsKey(balanceAddr))
                    {
                        dict[balanceAddr] = new Dictionary<Currency, FungibleAssetValue>();
                    }

                    if (dict[balanceAddr].ContainsKey(value.Currency))
                    {
                        dict[balanceAddr][value.Currency] += value;
                    }
                    else
                    {
                        dict[balanceAddr][value.Currency] = value;
                    }
                }

#pragma warning disable LAA1002
                var arr = dict
#pragma warning restore LAA1002
                    .Select(pair => (
                        balanceAddr: pair.Key,
                        value: pair.Value.Values.ToArray()))
                    .SelectMany(tuple =>
                        tuple.value.Select(value => (tuple.balanceAddr, value)))
                    .Where(tuple => tuple.value.Sign != 0)
                    .ToArray();
                result.fungibleAssetValues = arr.Any()
                    ? arr
                        .OrderBy(tuple => tuple.balanceAddr)
                        .ThenBy(tuple => tuple.value.GetHashCode())
                    : null;
            }

            result.inventoryAddr = inventoryAddr;

            var fiArr = fungibleIdAndCounts as (HashDigest<SHA256> fungibleId, int count)[] ??
                        fungibleIdAndCounts?.ToArray();
            if (fiArr is null ||
                fiArr.Length == 0)
            {
                result.fungibleIdAndCounts = null;
            }
            else
            {
                var dict = new Dictionary<HashDigest<SHA256>, int>();
                foreach (var (fungibleId, count) in fiArr)
                {
                    if (dict.ContainsKey(fungibleId))
                    {
                        dict[fungibleId] += count;
                    }
                    else
                    {
                        dict[fungibleId] = count;
                    }
                }

#pragma warning disable LAA1002
                var arr = dict
#pragma warning restore LAA1002
                    .Select(pair => (fungibleId: pair.Key, count: pair.Value))
                    .Where(tuple => tuple.count != 0)
                    .ToArray();
                result.fungibleIdAndCounts = arr.Any()
                    ? arr
                        .OrderBy(tuple => tuple.fungibleId.GetHashCode())
                        .ThenBy(tuple => tuple.count)
                    : null;
            }

            result.memo = string.IsNullOrEmpty(memo)
                ? null
                : memo;
            return result;
        }

        public static IOrderedEnumerable<(
            HashDigest<SHA256> fungibleId,
            int count,
            Address garageAddr,
            IValue? garageState)> WithGarageStateTuples(
            Address agentAddr,
            IAccountStateView states,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts)
        {
            var withGarageAddr = fungibleIdAndCounts
                .Select(tuple =>
                {
                    var garageAddr = Addresses.GetGarageAddress(
                        agentAddr,
                        tuple.fungibleId);
                    return (tuple.fungibleId, tuple.count, garageAddr);
                }).ToArray();
            var garageAddresses = withGarageAddr.Select(tuple => tuple.garageAddr).ToArray();
            var garageStates = states.GetStates(garageAddresses);
            return withGarageAddr
                .Zip(garageStates, (tuple, garageState) => (
                    tuple.fungibleId,
                    tuple.count,
                    tuple.garageAddr,
                    garageState))
                .OrderBy(tuple => tuple.fungibleId.GetHashCode());
        }

        public static IOrderedEnumerable<(
            HashDigest<SHA256> fungibleId,
            int count,
            Address senderGarageAddr,
            FungibleItemGarage senderGarage,
            Address recipientGarageAddr,
            IValue? recipientGarageState)> WithGarageStateTuples(
            Address senderAgentAddr,
            Address recipientAgentAddr,
            IAccountStateView states,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts)
        {
            var withGarageAddr = fungibleIdAndCounts
                .Select(tuple =>
                {
                    var senderGarageAddr = Addresses.GetGarageAddress(
                        senderAgentAddr,
                        tuple.fungibleId);
                    var recipientGarageAddr = Addresses.GetGarageAddress(
                        recipientAgentAddr,
                        tuple.fungibleId);
                    return (
                        tuple.fungibleId,
                        tuple.count,
                        senderGarageAddr,
                        recipientGarageAddr);
                }).ToArray();
            var garageAddresses = withGarageAddr
                .Select(tuple => tuple.senderGarageAddr)
                .Concat(withGarageAddr.Select(tuple => tuple.recipientGarageAddr))
                .ToArray();
            var garageStates = states.GetStates(garageAddresses);
            if (garageStates.Count != garageAddresses.Length)
            {
                throw new Exception($"garageStates.Count({garageStates.Count}) != " +
                                    $"garageAddresses.Length({garageAddresses.Length})");
            }

            var garageStatesHalfCount = garageStates.Count / 2;
            return withGarageAddr
                .Select((tuple, index) =>
                {
                    var senderGarageState = garageStates[index];
                    var senderGarage = ConvertToFungibleItemGarage(
                        senderGarageState,
                        tuple.senderGarageAddr,
                        tuple.fungibleId);
                    return (
                        tuple.fungibleId,
                        tuple.count,
                        tuple.senderGarageAddr,
                        senderGarage,
                        tuple.recipientGarageAddr,
                        recipientGarageState: garageStates[index + garageStatesHalfCount]);
                })
                .OrderBy(tuple => tuple.fungibleId.GetHashCode());
        }

        public static IOrderedEnumerable<(
            HashDigest<SHA256> fungibleId,
            int count,
            Address garageAddr,
            FungibleItemGarage garage)> WithGarageTuples(
            Address agentAddr,
            IAccountStateView states,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts)
        {
            return WithGarageStateTuples(agentAddr, states, fungibleIdAndCounts)
                .Select(tuple =>
                {
                    var (fungibleId, count, garageAddr, garageState) = tuple;
                    var garage = ConvertToFungibleItemGarage(
                        garageState,
                        garageAddr,
                        fungibleId);
                    return (
                        fungibleId,
                        count,
                        garageAddr,
                        garage);
                })
                .OrderBy(tuple => tuple.fungibleId.GetHashCode());
        }

        public static FungibleItemGarage ConvertToFungibleItemGarage(
            IValue? garageState,
            Address garageAddr,
            HashDigest<SHA256> fungibleId)
        {
            if (garageState is null || garageState is Null)
            {
                throw new StateNullException(garageAddr);
            }

            var garage = new FungibleItemGarage(garageState);
            if (!garage.Item.FungibleId.Equals(fungibleId))
            {
                throw new Exception(
                    $"{garageAddr} is not a garage of {fungibleId}.");
            }

            return garage;
        }
    }
}

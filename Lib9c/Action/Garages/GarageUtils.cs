#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Model.Garages;

namespace Nekoyume.Action.Garages
{
    public static class GarageUtils
    {
        public static IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            MergeAndSort(
                IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues)
        {
            var favArr = fungibleAssetValues as (Address balanceAddr, FungibleAssetValue value)[] ??
                         fungibleAssetValues?.ToArray();
            if (favArr is null ||
                favArr.Length == 0)
            {
                return null;
            }

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
            return arr.Any()
                ? arr
                    .OrderBy(tuple => tuple.balanceAddr)
                    .ThenBy(tuple => tuple.value.GetHashCode())
                : null;
        }

        public static IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            MergeAndSort(
                IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            var fiArr = fungibleIdAndCounts as (HashDigest<SHA256> fungibleId, int count)[] ??
                        fungibleIdAndCounts?.ToArray();
            if (fiArr is null ||
                fiArr.Length == 0)
            {
                return null;
            }

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
            return arr.Any()
                ? arr
                    .OrderBy(tuple => tuple.fungibleId.GetHashCode())
                    .ThenBy(tuple => tuple.count)
                : null;
        }

        public static IOrderedEnumerable<(
            HashDigest<SHA256> fungibleId,
            int count,
            Address garageAddr,
            IValue? garageState)> WithGarageStateTuples(
            Address agentAddr,
            IAccountState states,
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
            IAccountState states,
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
            IAccountState states,
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

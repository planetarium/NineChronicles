#nullable enable

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
using Nekoyume.Exceptions;
using Nekoyume.Model.Garages;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("deliver_to_others_garages")]
    public class DeliverToOthersGarages : GameAction, IDeliverToOthersGaragesV1, IAction
    {
        public Address RecipientAgentAddr { get; private set; }
        public IOrderedEnumerable<FungibleAssetValue>? FungibleAssetValues { get; private set; }

        public IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts { get; private set; }

        public string? Memo { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                {
                    "l",
                    new List(
                        RecipientAgentAddr.Serialize(),
                        FungibleAssetValues is null
                            ? (IValue)Null.Value
                            : new List(FungibleAssetValues.Select(fav => fav.Serialize())),
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

        public DeliverToOthersGarages(
            Address recipientAgentAddr,
            IEnumerable<FungibleAssetValue>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            RecipientAgentAddr = recipientAgentAddr;
            SetInternal(fungibleAssetValues, fungibleIdAndCounts, memo);
        }

        public DeliverToOthersGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var list = (List)plainValue["l"];
            RecipientAgentAddr = list[0].ToAddress();
            var fungibleAssetValues = list[1].Kind == ValueKind.Null
                ? null
                : ((List)list[1]).Select(e => e.ToFungibleAssetValue());
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
            SetInternal(fungibleAssetValues, fungibleIdAndCounts, memo);
        }

        private void SetInternal(
            IEnumerable<FungibleAssetValue>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            var favArr = fungibleAssetValues as FungibleAssetValue[] ??
                         fungibleAssetValues?.ToArray();
            if (favArr is null ||
                favArr.Length == 0)
            {
                FungibleAssetValues = null;
            }
            else
            {
                var dict = new Dictionary<Currency, FungibleAssetValue>();
                foreach (var fav in favArr)
                {
                    if (dict.ContainsKey(fav.Currency))
                    {
                        dict[fav.Currency] += fav;
                    }
                    else
                    {
                        dict[fav.Currency] = fav;
                    }
                }

                var arr = dict.Values
                    .Where(fav => fav.Sign != 0)
                    .ToArray();
                FungibleAssetValues = arr.Any()
                    ? arr.OrderBy(fav => fav.GetHashCode())
                    : null;
            }

            var fiArr = fungibleIdAndCounts as (HashDigest<SHA256> fungibleId, int count)[] ??
                        fungibleIdAndCounts?.ToArray();
            if (fiArr is null ||
                fiArr.Length == 0)
            {
                FungibleIdAndCounts = null;
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
                FungibleIdAndCounts = arr.Any()
                    ? arr
                        .OrderBy(tuple => tuple.fungibleId.GetHashCode())
                        .ThenBy(tuple => tuple.count)
                    : null;
            }

            Memo = string.IsNullOrEmpty(memo)
                ? null
                : memo;
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
            state = SendBalances(context, state);
            return SendFungibleItems(context.Signer, state);
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
                foreach (var fav in FungibleAssetValues)
                {
                    if (fav.Sign < 0)
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

        private IAccountStateDelta SendBalances(
            IActionContext context,
            IAccountStateDelta states)
        {
            if (FungibleAssetValues is null)
            {
                return states;
            }

            var senderGarageBalanceAddress =
                Addresses.GetGarageBalanceAddress(context.Signer);
            var recipientGarageBalanceAddr =
                Addresses.GetGarageBalanceAddress(RecipientAgentAddr);
            foreach (var fav in FungibleAssetValues)
            {
                states = states.TransferAsset(
                    context,
                    senderGarageBalanceAddress,
                    recipientGarageBalanceAddr,
                    fav);
            }

            return states;
        }

        private IAccountStateDelta SendFungibleItems(
            Address signer,
            IAccountStateDelta states)
        {
            if (FungibleIdAndCounts is null)
            {
                return states;
            }

            var fungibleItemTuples = GarageUtils.WithGarageStateTuples(
                signer,
                RecipientAgentAddr,
                states,
                FungibleIdAndCounts);
            foreach (var (
                         _,
                         count,
                         senderGarageAddr,
                         senderGarage,
                         recipientGarageAddr,
                         recipientGarageState) in
                     fungibleItemTuples)
            {
                var recipientGarage =
                    recipientGarageState is null || recipientGarageState is Null
                        ? new FungibleItemGarage(senderGarage.Item, 0)
                        : new FungibleItemGarage(recipientGarageState);
                senderGarage.Deliver(recipientGarage, count);
                states = states
                    .SetState(senderGarageAddr, senderGarage.Serialize())
                    .SetState(recipientGarageAddr, recipientGarage.Serialize());
            }

            return states;
        }
    }
}

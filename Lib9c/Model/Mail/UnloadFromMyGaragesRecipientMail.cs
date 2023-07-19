#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Garages;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    public class UnloadFromMyGaragesRecipientMail : Mail
    {
        protected override string TypeId => nameof(UnloadFromMyGaragesRecipientMail);
        public override MailType MailType => MailType.Auction;

        public readonly IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues;

        public readonly IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts;

        public readonly string? Memo;

        public UnloadFromMyGaragesRecipientMail(
            long blockIndex,
            Guid id,
            long requiredBlockIndex,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValue,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCount,
            string? memo)
            : base(blockIndex, id, requiredBlockIndex)
        {
            if (fungibleAssetValue is
                IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)> oe)
            {
                FungibleAssetValues = oe;
            }
            else
            {
                FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValue);
            }

            if (fungibleIdAndCount is
                IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)> oe2)
            {
                FungibleIdAndCounts = oe2;
            }
            else
            {
                FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCount);
            }

            Memo = memo;
        }

        public UnloadFromMyGaragesRecipientMail(Dictionary serialized) :
            base(serialized)
        {
            var list = (List)serialized["l"];
            var fungibleAssetValues = list[0].Kind == ValueKind.Null
                ? null
                : ((List)list[0]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToAddress(),
                        l2[1].ToFungibleAssetValue()
                    );
                });
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            var fungibleIdAndCounts = list[1].Kind == ValueKind.Null
                ? null
                : ((List)list[1]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToItemId(),
                        (int)((Integer)l2[1]).Value);
                });
            FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCounts);
            Memo = list[2].Kind == ValueKind.Null
                ? null
                : (string)(Text)list[2];
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override IValue Serialize()
        {
            var dict = (Dictionary)base.Serialize();
            return dict.SetItem("l", new List(
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
                Memo is null
                    ? (IValue)Null.Value
                    : (Text)Memo));
        }
    }
}

using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Crypto;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RedeemCodeListSheet : Sheet<PublicKey, RedeemCodeListSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<PublicKey>
        {
            public override PublicKey Key => _key;
            private PublicKey _key;
            public int RewardId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                _key = new PublicKey(ByteUtil.ParseHex(fields[0]));
                RewardId = ParseInt(fields[1]);
            }
        }

        public RedeemCodeListSheet() : base(nameof(RedeemCodeListSheet))
        {
        }
    }
}

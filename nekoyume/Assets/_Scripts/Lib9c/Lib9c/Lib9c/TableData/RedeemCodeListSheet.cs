using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class RedeemCodeListSheet : Sheet<int, RedeemCodeListSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int RewardId { get; private set; }
            public PublicKey PublicKey => PublicKeyBinary.ToPublicKey();
            public Binary PublicKeyBinary { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RewardId = ParseInt(fields[1]);
                PublicKeyBinary = ByteUtil.ParseHex(fields[2]);
            }
        }

        public RedeemCodeListSheet() : base(nameof(RedeemCodeListSheet))
        {
        }
    }
}

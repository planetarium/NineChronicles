using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Crypto;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ActivationSheet : Sheet<int, ActivationSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public PublicKey PublicKey { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                TryParseInt(fields[0], out var id);
                Id = id;
                PublicKey = new PublicKey(ByteUtil.ParseHex(fields[1]));
            }
        }

        public ActivationSheet() : base(nameof(ActivationSheet))
        {
        }
    }
}

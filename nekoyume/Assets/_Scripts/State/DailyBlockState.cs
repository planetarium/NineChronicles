using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.State
{
    [Serializable]
    public class DailyBlockState : State
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x2
            }
        );

        public readonly long nextBlockIndex;

        // 86400 / block interval
        public const long UpdateInterval = 8640;

        public DailyBlockState(long index) : base(Address)
        {
            nextBlockIndex = index + UpdateInterval;
        }

        public DailyBlockState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            nextBlockIndex = (long) ((Integer) serialized[(Text) "nextBlockIndex"]).Value;
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "nextBlockIndex"] = (Integer) nextBlockIndex,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}

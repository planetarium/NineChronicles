using System;
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
    }
}

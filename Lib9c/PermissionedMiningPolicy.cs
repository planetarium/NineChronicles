using Libplanet;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Nekoyume.BlockChain
{
    public struct PermissionedMiningPolicy
    {
        public PermissionedMiningPolicy(ISet<Address> miners, long threshold)
        {
            Miners = miners;
            Threshold = threshold;
        }

        public ISet<Address> Miners { get; private set; }

        public long Threshold { get; private set; }


        public static PermissionedMiningPolicy Mainnet => new PermissionedMiningPolicy()
        {
            Miners = new[]
            {
                new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
                new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
                new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
                new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
            }.ToImmutableHashSet(),
            Threshold = 2_225_500
        };
    }
}

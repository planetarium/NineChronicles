using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Action.Interface;

namespace Lib9c.DevExtensions.Action.Factory
{
    public class FaucetFactory
    {
        public static IFaucet CreateFaucetCurrency(
            Address agentAddress,
            int faucetNcg,
            int faucetCrystal
        )
        {
            return new FaucetCurrency(agentAddress, faucetNcg, faucetCrystal);
        }

        public static IFaucet CreateFaucetRune(
            Address avatarAddress, Dictionary<int, int> faucetRunes)
        {
            return new FaucetRune(avatarAddress, faucetRunes);
        }
    }
}

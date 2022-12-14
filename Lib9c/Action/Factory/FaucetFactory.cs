using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public class FaucetFactory
    {
        public static IFaucet CreateFaucet(
            Address agentAddress,
            int faucetNcg,
            int faucetCrystal
        )
        {
            return new Faucet(agentAddress, faucetNcg, faucetCrystal);
        }
    }
}

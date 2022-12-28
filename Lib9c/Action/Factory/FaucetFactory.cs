using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.Faucet;

namespace Nekoyume.Action.Factory
{
    public static class FaucetFactory
    {
        public static GameAction CreateFaucetCurrency(
            Address agentAddress,
            int faucetNcg,
            int faucetCrystal
        )
        {
            return new FaucetCurrency
            {
                AgentAddress = agentAddress,
                FaucetNcg = faucetNcg,
                FaucetCrystal = faucetCrystal,
            };
        }

        public static GameAction CreateFaucetRune(
            Address avatarAddress, List<FaucetRuneInfo> faucetRunes)
        {
            return new FaucetRune
            {
                AvatarAddress = avatarAddress,
                FaucetRuneInfos = faucetRunes,
            };
        }
    }
}

using System.Collections.Generic;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet;
using Nekoyume.Model.Faucet;

namespace Lib9c.DevExtensions.Action.Factory
{
    public class FaucetFactory
    {
        public static IFaucetCurrency CreateFaucetCurrency(
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

        public static IFaucetRune CreateFaucetRune(
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

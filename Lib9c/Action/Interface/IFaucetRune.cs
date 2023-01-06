using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.Faucet;

namespace Nekoyume.Action.Interface
{
    public interface IFaucetRune
    {
        Address AvatarAddress { get; set; }
        List<FaucetRuneInfo> FaucetRuneInfos { get; set; }
    }
}

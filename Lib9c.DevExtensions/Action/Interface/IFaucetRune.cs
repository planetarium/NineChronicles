using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Model.Faucet;

namespace Lib9c.DevExtensions.Action.Interface
{
    public interface IFaucetRune
    {
        Address AvatarAddress { get; set; }
        List<FaucetRuneInfo> FaucetRuneInfos { get; set; }
    }
}

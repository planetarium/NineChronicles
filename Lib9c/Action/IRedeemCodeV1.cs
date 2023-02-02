using Libplanet;

namespace Nekoyume.Action
{
    public interface IRedeemCodeV1
    {
        string Code { get; }
        Address AvatarAddress { get; }
    }
}

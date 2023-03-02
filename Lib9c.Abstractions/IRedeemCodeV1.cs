using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IRedeemCodeV1
    {
        string Code { get; }
        Address AvatarAddress { get; }
    }
}

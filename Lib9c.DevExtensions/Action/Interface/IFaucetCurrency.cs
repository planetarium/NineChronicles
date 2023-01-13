using Libplanet;

namespace Lib9c.DevExtensions.Action.Interface
{
    public interface IFaucetCurrency
    {
        Address AgentAddress { get; set; }
        int FaucetNcg { get; set; }
        int FaucetCrystal { get; set; }
    }
}

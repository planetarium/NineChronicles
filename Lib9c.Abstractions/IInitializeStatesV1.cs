#nullable enable
using System.Collections.Generic;
using Bencodex.Types;

namespace Lib9c.Abstractions
{
    public interface IInitializeStatesV1
    {
        Dictionary Ranking { get; }
        Dictionary Shop { get; }
        Dictionary<string, string> TableSheets { get; }
        Dictionary GameConfig { get; }
        Dictionary RedeemCode { get; }
        Dictionary AdminAddressState { get; }
        Dictionary ActivatedAccounts { get; }
        Dictionary GoldCurrency { get; }
        List GoldDistributions { get; }
        List PendingActivations { get; }
        Dictionary? AuthorizedMiners { get; }
        Dictionary? Credits { get; }
    }
}

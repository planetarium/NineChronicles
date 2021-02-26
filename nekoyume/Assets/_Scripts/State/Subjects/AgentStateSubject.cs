using System.Collections.Generic;
using System.Numerics;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    /// <summary>
    /// The change of the value included in `AgentState` is notified to the outside through each Subject<T> field.
    /// </summary>
    public static class AgentStateSubject
    {
        public static readonly Subject<FungibleAssetValue> Gold
            = new Subject<FungibleAssetValue>();
    }
}

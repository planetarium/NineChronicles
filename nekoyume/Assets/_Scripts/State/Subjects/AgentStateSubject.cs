using System.Collections.Generic;
using System.Numerics;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    /// <summary>
    /// AgentState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class AgentStateSubject
    {
        public static readonly Subject<FungibleAssetValue> Gold
            = new Subject<FungibleAssetValue>();
    }
}

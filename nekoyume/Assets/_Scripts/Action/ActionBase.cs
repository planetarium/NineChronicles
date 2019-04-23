using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public struct ErrorCode
        {
            public const int Success = 0;
            public const int Fail = -1;
            public const int KeyNotFoundInTable = -2;
        }
        public abstract IImmutableDictionary<string, object> PlainValue { get; }
        public abstract void LoadPlainValue(IImmutableDictionary<string, object> plainValue);
        public abstract IAccountStateDelta Execute(IActionContext context);
    }
}

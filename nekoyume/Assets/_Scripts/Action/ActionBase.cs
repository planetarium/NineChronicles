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

        public static readonly Address ProcessedActionsAddress = new Address(
            new byte[20]
            {
                0x45, 0xa2, 0x21, 0x87, 0xe2, 0xd8, 0x85, 0x0b, 0xb3, 0x57,
                0x88, 0x69, 0x58, 0xbc, 0x3e, 0x85, 0x60, 0x92, 0x9c, 0xcc,
            }
        );

        public Guid Id { get; internal set; }
        public void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Id = new Guid((string) plainValue["id"]);
            LoadPlainValueInternal(plainValue);
        }
        protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue);

        public IAccountStateDelta Execute(IActionContext ctx) 
        {
            var processedActions = (HashSet<Guid>) ctx.PreviousStates.GetState(ProcessedActionsAddress);

            if (ReferenceEquals(processedActions, null)) 
            {
                processedActions = new HashSet<Guid>();
            }

            if (processedActions.Contains(Id)) 
            {
                // 이미 액션이 처리된 것으로 간주하고 아무 일도 하지 않습니다.
                return ctx.PreviousStates;
            }

            IAccountStateDelta delta = ExecuteInternal(ctx);

            if (Id.Equals(Guid.Empty)) 
            {
                return delta;
            }

            processedActions.Add(Id);
            
            return delta.SetState(ProcessedActionsAddress, processedActions);
        }

        protected abstract IAccountStateDelta ExecuteInternal(IActionContext ctx);

        public IImmutableDictionary<string, object> PlainValue
        { 
            get 
            {
                return PlainValueInternal.SetItem("id", Id.ToString());
            }    
        }
        protected abstract IImmutableDictionary<string, object> PlainValueInternal { get; }
    }
}

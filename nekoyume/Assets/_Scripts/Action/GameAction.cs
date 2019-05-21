using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    public abstract class GameAction : ActionBase
    {
        public static readonly Address ProcessedActionsAddress = new Address(
            new byte[20]
            {
                0x45, 0xa2, 0x21, 0x87, 0xe2, 0xd8, 0x85, 0x0b, 0xb3, 0x57,
                0x88, 0x69, 0x58, 0xbc, 0x3e, 0x85, 0x60, 0x92, 0x9c, 0xcc,
            }
        );

        public Guid Id { get; internal set; }

        public GameAction()
        {
            Id = Guid.NewGuid();
        }

        protected static IAccountStateDelta SimpleError(IActionContext actionCtx, AvatarState ctx, int errorCode)
        {
            ctx.updatedAt = DateTimeOffset.UtcNow;
            ctx.SetGameActionResult(new GameActionResult
            {
                errorCode = errorCode,
            });
                    
            return actionCtx.PreviousStates.SetState(actionCtx.Signer, ctx);
        }

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Id = new Guid((string) plainValue["id"]);
            LoadPlainValueInternal(plainValue);
        }
        protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue);

        public override IAccountStateDelta Execute(IActionContext ctx) 
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
            processedActions.Add(Id);
            
            return delta.SetState(ProcessedActionsAddress, processedActions);
        }

        protected abstract IAccountStateDelta ExecuteInternal(IActionContext ctx);
        
        public override IImmutableDictionary<string, object> PlainValue
        { 
            get 
            {
                return PlainValueInternal.SetItem("id", Id.ToString());
            }    
        }
        protected abstract IImmutableDictionary<string, object> PlainValueInternal { get; }
        public const string MarkChanged = "";
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    public abstract partial class GameAction : ActionBase
    {
        public static readonly Address ProcessedActionsAddress = new Address(
            new byte[20]
            {
                0x45, 0xa2, 0x21, 0x87, 0xe2, 0xd8, 0x85, 0x0b, 0xb3, 0x57,
                0x88, 0x69, 0x58, 0xbc, 0x3e, 0x85, 0x60, 0x92, 0x9c, 0xcc,
            }
        );

        public int errorCode = ErrorCode.Success;

        public Guid Id { get; private set; }
        public bool Succeed => errorCode == ErrorCode.Success;
        public override IImmutableDictionary<string, object> PlainValue => PlainValueInternal.SetItem("id", Id.ToString());
        protected abstract IImmutableDictionary<string, object> PlainValueInternal { get; }
        
        protected GameAction()
        {
            Id = Guid.NewGuid();
        }

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            Id = new Guid((string) plainValue["id"]);
            LoadPlainValueInternal(plainValue);
        }
        
        protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue);
                
        protected IAccountStateDelta SimpleError(IActionContext ctx, int code)
        {
            errorCode = code;
            return ctx.PreviousStates;
        }
        
        protected IAccountStateDelta SimpleAgentError(IActionContext ctx, Address address, AgentState agentState, int code)
        {
            errorCode = code;
            return ctx.PreviousStates.SetState(address, agentState);
        }
        
        protected IAccountStateDelta SimpleAvatarError(IActionContext ctx, Address address, AvatarState avatarState, int code)
        {
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            errorCode = code;
            return ctx.PreviousStates.SetState(address, avatarState);
        }
    }
}

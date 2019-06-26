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
        // FixMe. 사용하지 않는 필드.
        public static readonly Address ProcessedActionsAddress = new Address(
            new byte[20]
            {
                0x45, 0xa2, 0x21, 0x87, 0xe2, 0xd8, 0x85, 0x0b, 0xb3, 0x57,
                0x88, 0x69, 0x58, 0xbc, 0x3e, 0x85, 0x60, 0x92, 0x9c, 0xcc,
            }
        );

        public Guid Id { get; private set; }
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
    }
}

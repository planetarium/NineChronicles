using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("move_stage")]
    public class MoveStage : ActionBase
    {
        public int stage;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            stage = int.Parse(plainValue["stage"].ToString());
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var avatar = (Avatar) states.GetValueOrDefault(to);
            avatar.WorldStage = stage;
            return (AddressStateMap) states.SetItem(to, avatar);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["stage"] = stage,
        }.ToImmutableDictionary();
    }
}

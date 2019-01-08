using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("sleep")]
    public class Sleep : ActionBase
    {
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var avatar = (Avatar) states.GetValueOrDefault(to);
            avatar.Dead = false;
            avatar.CurrentHP = avatar.HPMax;
            return (AddressStateMap) states.SetItem(to, avatar);
        }

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>().ToImmutableDictionary();
    }
}
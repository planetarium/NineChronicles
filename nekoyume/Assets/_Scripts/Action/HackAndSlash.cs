using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : ActionBase
    {
        public int hp;
        public int zone;
        public long exp;
        public int level;
        public bool dead;
        public string items;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            hp = int.Parse(plainValue["hp"].ToString());
            zone = int.Parse(plainValue["zone"].ToString());
            exp = long.Parse(plainValue["exp"].ToString());
            level = int.Parse(plainValue["level"].ToString());
            dead = (bool) plainValue["dead"];
            items = (string) plainValue["items"];
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var avatar = (Avatar) states.GetValueOrDefault(to);
            if (avatar.Dead)
            {
                throw new InvalidActionException();
            }

            avatar.CurrentHP = hp;
            avatar.WorldStage = zone;
            avatar.EXP = exp;
            avatar.Level = level;
            avatar.Dead = dead;
            avatar.Items = items;
            return (AddressStateMap) states.SetItem(to, avatar);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["hp"] = hp,
            ["zone"] = zone,
            ["exp"] = exp,
            ["level"] = level,
            ["dead"] = dead,
            ["items"] = items,
        }.ToImmutableDictionary();
    }
}

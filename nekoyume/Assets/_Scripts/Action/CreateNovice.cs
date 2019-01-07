using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    public class CreateNovice : ActionBase
    {
        private string _name;

        public CreateNovice(string name)
        {
            _name = name;
        }

        public override Dictionary<string, string> ToDetails()
        {
            return new Dictionary<string, string>
            {
                ["name"] = _name
            };
        }

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            _name = Encoding.UTF8.GetString((byte[]) plainValue["name"]);
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var result = states.GetValueOrDefault(to) ?? new Avatar
            {
                Name = _name,
                Level = 1,
                EXP = 0,
                HPMax = 0,
                WorldStage = 1,
                CurrentHP = 0,
            };
            return (AddressStateMap) states.SetItem(to, result);
        }
        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>()
        {
            ["name"] = _name,
        }.ToImmutableDictionary();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GameConfigState : State
    {
        public static readonly Address Address = Addresses.GameConfig;
        public int HourglassPerBlock { get; private set; }
        public int ActionPointMax { get; private set; }

        public GameConfigState() : base(Address)
        {
        }

        public GameConfigState(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "hourglass_per_block", out var value))
            {
                HourglassPerBlock = value.ToInteger();
            }
            if (serialized.TryGetValue((Text) "action_point_max", out var value2))
            {
                ActionPointMax = value2.ToInteger();
            }
        }

        public GameConfigState(string csv) : base(Address)
        {
            var sheet = new GameConfigSheet();
            sheet.Set(csv);
            foreach (var row in sheet.Values)
            {
                Update(row);
            }
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "hourglass_per_block"] = HourglassPerBlock.Serialize(),
                [(Text) "action_point_max"] = ActionPointMax.Serialize(),
            };
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
        }

        public void Update(GameConfigSheet.Row row)
        {
            switch (row.Key)
            {
                case "hourglass_per_block":
                    HourglassPerBlock = TableExtensions.ParseInt(row.Value);
                    break;
                case "action_point_max":
                    ActionPointMax = TableExtensions.ParseInt(row.Value);
                    break;
            }
        }
    }
}

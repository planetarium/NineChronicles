using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Resources;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Data.Table;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.State
{
    public class TableSheetsState : State
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3
            }
        );

        public ConsumableItemRecipeSheet ConsumableItemRecipeSheet
        {
            get
            {
                if (TableSheets.TryGetValue(nameof(ConsumableItemRecipeSheet), out string csv))
                {
                    var sheet = new ConsumableItemRecipeSheet();
                    sheet.Set(csv);
                    return sheet;
                }
                return null;
            }
        }

        // key = TableSheet Name / value = TableSheet csv.
        public Dictionary<string, string> TableSheets = new Dictionary<string, string>();

        public TableSheetsState() : base(Address)
        {
        }

        public TableSheetsState(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            TableSheets = serialized.GetValue<Bencodex.Types.Dictionary>("table_sheets")
                .ToDictionary(pair => (string) (Text) pair.Key, pair => (string) (Text) pair.Value);
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "table_sheets"] = new Bencodex.Types.Dictionary(TableSheets.Select(pair =>
                    new KeyValuePair<IKey, IValue>((Text) pair.Key, (Text) pair.Value)))
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

        public static TableSheetsState Current
        {
            get
            {
                var d = Game.Game.instance.agent.GetState(Address);
                if (d == null)
                {
                    return new TableSheetsState();
                }
                else
                {
                    return new TableSheetsState((Bencodex.Types.Dictionary)d);
                }
            }
        }

        public static TableSheetsState FromActionContext(IActionContext ctx)
        {
            var serialized = ctx.PreviousStates.GetState(Address);
            if (serialized == null)
            {
                return new TableSheetsState();
            }
            else
            {
                return new TableSheetsState((Bencodex.Types.Dictionary)serialized);
            }
        }
    }
}
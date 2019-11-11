using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType("patch_table_sheet")]
    public class PatchTableSheet : GameAction
    {
        public static ImmutableHashSet<Address> Administrators =
            ImmutableHashSet<Address>.Empty
                .Add(new Address("753a2b8297fcE1203d4F8bFAd41911F15D11af54"));
        
        public string TableName;
        public string TableCSV;
        
        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (!Administrators.Contains(ctx.Signer))
            {
                return states;
            }

            var tableSheetsState = TableSheetsState.FromActionContext(ctx);
            tableSheetsState.TableSheets[TableName] = TableCSV;
            return states.SetState(TableSheetsState.Address, tableSheetsState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("table_name", (Text) TableName)
                .SetItem("table_csv", (Text) TableCSV);

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            TableName = (Text) plainValue["table_name"];
            TableCSV = (Text) plainValue["table_csv"];
        }
    }
}
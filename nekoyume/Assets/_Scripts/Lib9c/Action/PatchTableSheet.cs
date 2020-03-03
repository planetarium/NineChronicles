using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("patch_table_sheet")]
    public class PatchTableSheet : GameAction
    {
        public static ImmutableHashSet<Address> Administrators => ImmutableHashSet<Address>.Empty;
        
        public string TableName;
        public string TableCsv;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var tableSheetsState = TableSheetsState.FromActionContext(ctx);
            Log.Debug($"[{ctx.BlockIndex}] {TableName} was patched by {ctx.Signer.ToHex()}\n" +
                      "before:\n" +
                      (tableSheetsState.TableSheets.TryGetValue(TableName, out string value) ? value : string.Empty) +
                      "\n" +
                      "after:\n" +
                      TableCsv
            );

            TableSheetsState nextState = tableSheetsState.UpdateTableSheet(TableName, TableCsv);
            return states.SetState(TableSheetsState.Address, nextState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("table_name", (Text) TableName)
                .SetItem("table_csv", (Text) TableCsv);

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            TableName = (Text) plainValue["table_name"];
            TableCsv = (Text) plainValue["table_csv"];
        }
    }
}

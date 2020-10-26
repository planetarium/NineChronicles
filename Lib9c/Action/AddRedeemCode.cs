using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_redeem_code")]
    public class AddRedeemCode : GameAction
    {
        public string redeemCsv;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states
                    .SetState(Addresses.RedeemCode, MarkChanged);
            }

            CheckPermission(context);

            var redeem = states.GetRedeemCodeState();
            var sheet = new RedeemCodeListSheet();
            sheet.Set(redeemCsv);
            redeem.Update(sheet);
            return states
                .SetState(Addresses.RedeemCode, redeem.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("redeem_csv", redeemCsv.Serialize());
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            redeemCsv = plainValue["redeem_csv"].ToDotnetString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("faucet_currency")]
    public class FaucetCurrency : GameAction, IFaucetCurrency
    {
        public Libplanet.Address AgentAddress { get; set; }
        public int FaucetNcg { get; set; }
        public int FaucetCrystal { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            if (FaucetNcg > 0)
            {
                var ncg = states.GetGoldCurrency();
                states = states.TransferAsset(GoldCurrencyState.Address, AgentAddress, ncg * FaucetNcg);
            }

            if (FaucetCrystal > 0)
            {
#pragma warning disable CS0618
                var crystal = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618
                states = states.MintAsset(AgentAddress, crystal * FaucetCrystal);
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["agentAddress"] = AgentAddress.Serialize(),
                ["faucetNcg"] = FaucetNcg.Serialize(),
                ["faucetCrystal"] = FaucetCrystal.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AgentAddress = plainValue["agentAddress"].ToAddress();
            if (plainValue.ContainsKey("faucetNcg"))
            {
                FaucetNcg = plainValue["faucetNcg"].ToInteger();
            }

            if (plainValue.ContainsKey("faucetCrystal"))
            {
                FaucetCrystal = plainValue["faucetCrystal"].ToInteger();
            }
        }
    }
}

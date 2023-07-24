using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("faucet_currency")]
    public class FaucetCurrency : GameAction, IFaucetCurrency
    {
        public Address AgentAddress { get; set; }
        public int FaucetNcg { get; set; }
        public int FaucetCrystal { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var states = context.PreviousState;
            if (FaucetNcg > 0)
            {
                var ncg = states.GetGoldCurrency();
                states = states.TransferAsset(context, GoldCurrencyState.Address, AgentAddress, ncg * FaucetNcg);
            }

            if (FaucetCrystal > 0)
            {
#pragma warning disable CS0618
                var crystal = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618
                states = states.MintAsset(context, AgentAddress, crystal * FaucetCrystal);
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

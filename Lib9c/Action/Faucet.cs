using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action.Interface;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("faucet")]
    public class Faucet : GameAction, IFaucet
    {
        public Address AgentAddress;
        public int FaucetNcg;
        public int FaucetCrystal;

        // public Address AvatarAddress;
        // public Dictionary<int, int> Runes = new Dictionary<int, int>();


        public Faucet(Address agentAddress, int faucetNcg = 0, int faucetCrystal = 0)
        {
            AgentAddress = agentAddress;
            FaucetNcg = faucetNcg;
            FaucetCrystal = faucetCrystal;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
// #if UNITY_EDITOR || TEST_9C
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            if (FaucetNcg > 0)
            {
                var ncg = Currency.Legacy("NCG", 2, null);
                states = states.MintAsset(AgentAddress, ncg * FaucetNcg);
            }

            if (FaucetCrystal > 0)
            {
                var crystal = Currency.Legacy("CRYSTAL", 18, null);
                states = states.MintAsset(AgentAddress, crystal * FaucetCrystal);
            }

            return states;
// #endif
            throw new ActionUnavailableException("This action is only for test.");
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal { get; }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AgentAddress = plainValue["agent_address"].ToAddress();
            if (plainValue.ContainsKey("ncg"))
            {
                FaucetNcg = plainValue["ncg"].ToInteger();
            }

            if (plainValue.ContainsKey("crystal"))
            {
                FaucetCrystal = plainValue["crystal"].ToInteger();
            }

            // AvatarAddress = plainValue["avatar_address"].ToAddress();
            // Runes = ((Dictionary)plainValue["runes"]).ToDictionary(pair =>
            // pair.Key.ToInteger(), pair => pair.Value.ToInteger());
        }
    }
}

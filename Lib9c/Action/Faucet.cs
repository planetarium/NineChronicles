using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action.Interface;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("faucet_currency")]
    public class FaucetCurrency : GameAction, IFaucet
    {
        public Address AgentAddress;
        public int FaucetNcg;
        public int FaucetCrystal;

        public FaucetCurrency(Address agentAddress, int faucetNcg = 0, int faucetCrystal = 0)
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

    [Serializable]
    [ActionType("faucet_rune")]
    public class FaucetRune : GameAction, IFaucet
    {
        public Address AvatarAddress;
        public Dictionary<int, int> FaucetRunes;

        public FaucetRune(Address avatarAddress, Dictionary<int, int> faucetRunes)
        {
            AvatarAddress = avatarAddress;
            FaucetRunes = faucetRunes;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
// #if TEST_9C
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            if (!(FaucetRunes is null))
            {
                RuneSheet runeSheet = states.GetSheet<RuneSheet>();
                if (runeSheet.OrderedList != null)
                {
                    foreach (var rune in FaucetRunes.OrderBy(x => x.Key))
                    {
                        states = states.MintAsset(AvatarAddress, RuneHelper.ToFungibleAssetValue(
                            runeSheet.OrderedList.First(r => r.Id == rune.Key),
                            rune.Value
                        ));
                    }
                }
            }

            return states;

// #endif
            throw new ActionUnavailableException("This action is only for test.");
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal { get; }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            throw new NotImplementedException();
        }
    }
}

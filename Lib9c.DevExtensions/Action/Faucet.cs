using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("faucet_currency")]
    public class FaucetCurrency : GameAction, IFaucet
    {
        public Libplanet.Address AgentAddress;
        public int FaucetNcg;
        public int FaucetCrystal;

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
                states = states.MintAsset(AgentAddress, ncg * FaucetNcg);
            }

            if (FaucetCrystal > 0)
            {
                var crystal = Currency.Legacy("CRYSTAL", 18, null);
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

    [Serializable]
    [ActionType("faucet_rune")]
    public class FaucetRune : GameAction, IFaucet
    {
        public Libplanet.Address AvatarAddress;
        public List<FaucetRuneInfo> FaucetRuneInfos;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            if (!(FaucetRuneInfos is null))
            {
                RuneSheet runeSheet = states.GetSheet<RuneSheet>();
                if (runeSheet.OrderedList != null)
                {
                    foreach (var rune in FaucetRuneInfos)
                    {
                        states = states.MintAsset(AvatarAddress, RuneHelper.ToFungibleAssetValue(
                            runeSheet.OrderedList.First(r => r.Id == rune.RuneId),
                            rune.Amount
                        ));
                    }
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["faucetRuneInfos"] = FaucetRuneInfos
                    .OrderBy(x => x.RuneId)
                    .Select(x => x.Serialize())
                    .Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            FaucetRuneInfos = plainValue["faucetRuneInfos"].ToList(
                x => new FaucetRuneInfo((List)x)
            );
        }
    }
}

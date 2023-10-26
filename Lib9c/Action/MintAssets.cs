using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class MintAssets : ActionBase
    {
        public const string TypeIdentifier = "mint_assets";
        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"type_id", (Text)TypeIdentifier),
                    new KeyValuePair<IKey, IValue>((Text)"values", new List(
                            FungibleAssetValues.Select(
                                r => new List(r.recipient.Bencoded, r.amount.Serialize()
                            )
                        )
                    ))
                }
            );

        public MintAssets()
        {
        }

        public MintAssets(
            IEnumerable<(Address recipient, FungibleAssetValue amount)> fungibleAssetValues
        )
        {
            FungibleAssetValues = fungibleAssetValues.ToList();
        }

        public override IAccount Execute(IActionContext context)
        {
            CheckPermission(context);
            IAccount state = context.PreviousState;

            foreach (var (recipient, amount) in FungibleAssetValues)
            {
                state = state.MintAsset(context, recipient, amount);
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            FungibleAssetValues = ((List)asDict["values"]).Select(v =>
            {
                var asList = (List)v;
                return (asList[0].ToAddress(), asList[1].ToFungibleAssetValue());
            }).ToList();
        }

        public List<(Address recipient, FungibleAssetValue amount)> FungibleAssetValues
        {
            get;
            private set;
        }
    }
}

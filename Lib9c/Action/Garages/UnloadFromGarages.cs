using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.Garages
{
    [ActionType("unload_from_garages")]
    public class UnloadFromGarages : GameAction, IUnloadFromGaragesV1, IAction
    {
        public IReadOnlyList<(
            Address recipientAvatarAddress,
            IReadOnlyList<(Address balanceAddress, FungibleAssetValue value)> fungibleAssetValues,
            IReadOnlyList<(HashDigest<SHA256> fungibleId, int count)> FungibleIdAndCounts)> UnloadData
        {
            get;
        }

        public string Memo { get; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal { get; }
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            throw new System.NotImplementedException();
        }

        public override IAccount Execute(IActionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}

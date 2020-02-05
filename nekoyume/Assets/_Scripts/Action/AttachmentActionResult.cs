using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class AttachmentActionResult : IState
    {
        private static readonly Dictionary<string, Func<Dictionary, AttachmentActionResult>>
            Deserializers = new Dictionary<string, Func<Dictionary, AttachmentActionResult>>
            {
                ["buy.buyerResult"] = d => new Buy.BuyerResult(d),
                ["buy.sellerResult"] = d => new Buy.SellerResult(d),
                ["combination.result-model"] = d => new Combination.ResultModel(d),
                ["itemEnhancement.result"] = d => new ItemEnhancement.ResultModel(d),
                ["sellCancellation.result"] = d => new SellCancellation.Result(d),
            };

        public ItemUsable itemUsable;

        protected abstract string TypeId { get; }

        protected AttachmentActionResult()
        {
            if (!Deserializers.ContainsKey(TypeId))
            {
                throw new InvalidOperationException(
                    $"Deserializer for the typeId \"{TypeId}\" seems not registered yet."
                );
            }
        }

        protected AttachmentActionResult(Bencodex.Types.Dictionary serialized)
        {
            itemUsable = (ItemUsable) ItemFactory.Deserialize(
                (Bencodex.Types.Dictionary) serialized["itemUsable"]
            );
        }

        public virtual IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "typeId"] = (Text) TypeId,
                [(Text) "itemUsable"] = itemUsable.Serialize(),
            });

        public static AttachmentActionResult Deserialize(Bencodex.Types.Dictionary serialized)
        {
            string typeId = ((Text) serialized["typeId"]).Value;
            Func<Dictionary, AttachmentActionResult> deserializer;
            try
            {
                deserializer = Deserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    Deserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Log.Error("{0} was raised during deserialize: {1}", e.GetType().FullName, serialized);
                throw;
            }
        }
    }
}

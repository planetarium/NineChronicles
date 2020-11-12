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
                ["combination.result-model"] = d => new CombinationConsumable.ResultModel(d),
                ["itemEnhancement.result"] = d => new ItemEnhancement.ResultModel(d),
                ["sellCancellation.result"] = d => new SellCancellation.Result(d),
                ["rapidCombination.result"] = d => new RapidCombination.ResultModel(d),
            };

        public ItemUsable itemUsable;
        public Costume costume; 

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

        protected AttachmentActionResult(Dictionary serialized)
        {
            itemUsable = serialized.ContainsKey("itemUsable")
               ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized["itemUsable"])
               : null;
            costume = serialized.ContainsKey("costume")
                ? (Costume) ItemFactory.Deserialize((Dictionary) serialized["costume"])
                : null;
        }

        public virtual IValue Serialize()
        {
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(Text) "typeId"] = (Text) TypeId, 
            };
            
            if (itemUsable != null)
            {
                innerDictionary.Add((Text) "itemUsable", itemUsable.Serialize());
            }

            if (costume != null)
            {
                innerDictionary.Add((Text) "costume", costume.Serialize());
            }
            
            return new Dictionary(innerDictionary);
        }
        
        public virtual IValue SerializeBackup1() =>
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

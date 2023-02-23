using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;
using BxDictionary = Bencodex.Types.Dictionary;
using BxText = Bencodex.Types.Text;

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class AttachmentActionResult : IState
    {
        private static readonly Dictionary<string, Func<BxDictionary, AttachmentActionResult>>
            Deserializers = new Dictionary<string, Func<BxDictionary, AttachmentActionResult>>
            {
                ["buy.buyerResult"] = d => new Buy7.BuyerResult(d),
                ["buy.sellerResult"] = d => new Buy7.SellerResult(d),
                ["combination.result-model"] = d => new CombinationConsumable5.ResultModel(d),
                ["itemEnhancement.result"] = d => new ItemEnhancement7.ResultModel(d),
                ["item_enhancement9.result"] = d => new ItemEnhancement9.ResultModel(d),
                ["item_enhancement11.result"] = d => new ItemEnhancement.ResultModel(d),
                ["sellCancellation.result"] = d => new SellCancellation.Result(d),
                ["rapidCombination.result"] = d => new RapidCombination0.ResultModel(d),
                ["rapid_combination5.result"] = d => new RapidCombination5.ResultModel(d),
                ["dailyReward.dailyRewardResult"] = d => new DailyReward2.DailyRewardResult(d),
                ["monsterCollection.result"] = d => new MonsterCollectionResult(d),
            };

        public ItemUsable itemUsable;
        public Costume costume;
        public ITradableFungibleItem tradableFungibleItem;
        public int tradableFungibleItemCount;

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

        protected AttachmentActionResult(BxDictionary serialized)
        {
            itemUsable = serialized.ContainsKey("itemUsable")
               ? (ItemUsable) ItemFactory.Deserialize((BxDictionary) serialized["itemUsable"])
               : null;
            costume = serialized.ContainsKey("costume")
                ? (Costume) ItemFactory.Deserialize((BxDictionary) serialized["costume"])
                : null;
            tradableFungibleItem = serialized.ContainsKey("tradableFungibleItem")
                ? (ITradableFungibleItem) ItemFactory.Deserialize(
                    (BxDictionary) serialized["tradableFungibleItem"])
                : null;
            tradableFungibleItemCount = serialized.ContainsKey("tradableFungibleItemCount")
                ? serialized["tradableFungibleItemCount"].ToInteger()
                : default;
        }

        public virtual IValue Serialize()
        {
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(BxText) "typeId"] = (BxText) TypeId,
            };

            if (itemUsable != null)
            {
                innerDictionary.Add((BxText) "itemUsable", itemUsable.Serialize());
            }

            if (costume != null)
            {
                innerDictionary.Add((BxText) "costume", costume.Serialize());
            }

            if (tradableFungibleItem != null)
            {
                innerDictionary.Add(
                    (BxText) "tradableFungibleItem",
                    tradableFungibleItem.Serialize());
                innerDictionary.Add(
                    (BxText) "tradableFungibleItemCount",
                    tradableFungibleItemCount.Serialize());
            }

            return new BxDictionary(innerDictionary);
        }

        public virtual IValue SerializeBackup1() =>
            new BxDictionary(new Dictionary<IKey, IValue>
            {
                [(BxText) "typeId"] = (BxText) TypeId,
                [(BxText) "itemUsable"] = itemUsable.Serialize(),
            });

        public static AttachmentActionResult Deserialize(BxDictionary serialized)
        {
            var typeId = ((BxText) serialized["typeId"]).Value;
            Func<BxDictionary, AttachmentActionResult> deserializer;
            try
            {
                deserializer = Deserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                var typeIds = string.Join(
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
                Log.Error(
                    "{FullName} was raised during deserialize: {Serialized}",
                    e.GetType().FullName,
                    serialized);
                throw;
            }
        }
    }
}

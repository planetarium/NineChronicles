#nullable enable

using System;
using Bencodex.Types;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.Garages
{
    public class FungibleItemGarage : IFungibleItemGarage
    {
        public IFungibleItem Item { get; }
        public int Count { get; private set; }

        public FungibleItemGarage(IFungibleItem item, int count)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Count must be greater than or equal to 0.");
            }

            Count = count;
        }

        public FungibleItemGarage(IValue? serialized)
        {
            if (serialized is null || serialized is Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            var list = (List)serialized;
            Item = list[0].Kind == ValueKind.Null
                ? throw new ArgumentNullException(nameof(serialized), "Item is null.")
                : (IFungibleItem)ItemFactory.Deserialize((Dictionary)list[0]);
            Count = (Integer)list[1];
        }

        public IValue Serialize() => new List(
                Item?.Serialize() ?? Null.Value,
                (Integer)Count);

        public void Load(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be greater than or equal to 0.");
            }

            if (count > int.MaxValue - Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be less than or equal to {int.MaxValue - Count}.");
            }

            Count += count;
        }

        public void Unload(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be greater than or equal to 0.");
            }

            if (count > Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be less than or equal to {Count}.");
            }

            Count -= count;
        }

        public void Deliver(IFungibleItemGarage to, int count)
        {
            if (to is null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            // NOTE:
            // Why not compare the garage.Item with this.Item directly?
            // Because the ITradableFungibleItem.Equals() method compares the
            // ITradableItem.RequiredBlockIndex property.
            // The IFungibleItem.FungibleId property fully contains the
            // specification of the fungible item.
            // So ITradableItem.RequiredBlockIndex property does not considered
            // when transferring items via garage.
            if (!to.Item.FungibleId.Equals(Item.FungibleId))
            {
                throw new ArgumentException(
                    $"Item type mismatched. {to.Item.FungibleId} != {Item.FungibleId}",
                    nameof(to));
            }

            Unload(count);
            to.Load(count);
        }
    }
}

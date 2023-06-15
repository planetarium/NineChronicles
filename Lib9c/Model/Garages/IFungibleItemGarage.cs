using Nekoyume.Model.Item;

namespace Nekoyume.Model.Garages
{
    public interface IFungibleItemGarage : IGarage<IFungibleItemGarage, int>
    {
        IFungibleItem Item { get; }
        int Count { get; }
    }
}

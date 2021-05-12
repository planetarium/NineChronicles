using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public interface IItem: IState
    {
        ItemType ItemType { get; }

        ItemSubType ItemSubType { get; }
    }
}

using Libplanet.Action;

namespace Lib9c.DevExtensions.Action.Interface
{
    public interface ICreateOrReplaceAvatar : IAction
    {
        int AvatarIndex { get; }
        string Name { get; }
        int Hair { get; }
        int Lens { get; }
        int Ear { get; }
        int Tail { get; }
        int Level { get; }
        // (int itemId, int count)[] Consumables { get; }
        // (int itemId, int count)[] Costumes { get; }
        (int itemId, int enhancement)[] Equipments { get; }
        // (int itemId, int count)[] Materials { get; }
        // (int itemId, int enhancement)[] Runes { get; }
        // int AllCostumesCount { get; }
        // int AllConsumablesCount { get; }
        // int AllEquipmentsCount { get; }
        // int AllMaterialsCount { get; }
    }
}

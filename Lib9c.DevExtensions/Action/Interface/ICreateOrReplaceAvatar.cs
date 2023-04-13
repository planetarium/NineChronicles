using System.Linq;
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

        IOrderedEnumerable<(int equipmentId, int level)> Equipments { get; }
        IOrderedEnumerable<(int consumableId, int count)> Foods { get; }
        IOrderedEnumerable<int> CostumeIds { get; }
        IOrderedEnumerable<(int runeId, int level)> Runes { get; }
        (int stageId, IOrderedEnumerable<int> crystalRandomBuffIds)? CrystalRandomBuff { get; }
    }
}

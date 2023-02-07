using System.Collections.Generic;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IUnlockEquipmentRecipeV1
    {
        IEnumerable<int> RecipeIds { get; }
        Address AvatarAddress { get; }
    }
}

using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IUnlockEquipmentRecipeV1
    {
        IEnumerable<int> RecipeIds { get; }
        Address AvatarAddress { get; }
    }
}

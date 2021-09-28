using System;

namespace Nekoyume.UI
{
    [Serializable]
    public class CombinationRecipeGroup
    {
        public RecipeGroup[] Groups { get; set; }
    }

    [Serializable]
    public class RecipeGroup
    {
        public int Key { get; set; }

        public int Grade { get; set; }

        public int[] RecipeIds { get; set; }
    }
}

using System;

namespace Nekoyume.UI
{
    [Serializable]
    public class CombinationSubRecipeTabs
    {
        public SubRecipeTab[] SubRecipeTabs { get; set; }
    }

    [Serializable]
    public class SubRecipeTab
    {
        public int RecipeId { get; set; }

        public string[] TabNames { get; set; }
    }
}

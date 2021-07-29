using Nekoyume.UI.Model;

namespace Nekoyume.UI.Scroller
{
    public class RecipeScroll : RectScroll<RecipeRow.Model, RecipeScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }
    }
}

namespace Nekoyume.UI.Scroller
{
    public class CustomCraftStatScroll : RectScroll<CustomCraftStatCell.Model, CustomCraftStatScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public CustomCraftStatCell.Model CurrentModel;
        }
    }
}

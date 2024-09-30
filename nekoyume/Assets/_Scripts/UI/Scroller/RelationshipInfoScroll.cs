namespace Nekoyume.UI.Scroller
{
    public class RelationshipInfoScroll : RectScroll<RelationshipInfoCell.Model, RelationshipInfoScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public RelationshipInfoCell.Model CurrentModel;
        }

        public RelationshipInfoCell.Model CurrentModel
        {
            set => Context.CurrentModel = value;
        }
    }
}

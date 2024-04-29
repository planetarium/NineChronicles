namespace Nekoyume.UI.Scroller
{
    public class RuneLevelBonusEffectScroll : RectScroll<RuneLevelBonusEffectCell.Model, RuneLevelBonusEffectScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public RuneLevelBonusEffectCell.Model CurrentModel;
        }

        public RuneLevelBonusEffectCell.Model CurrentModel
        {
            set => Context.CurrentModel = value;
        }
    }
}

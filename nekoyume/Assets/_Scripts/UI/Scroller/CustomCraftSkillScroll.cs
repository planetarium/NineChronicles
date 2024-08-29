using System;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CustomCraftSkillScroll : RectScroll<CustomCraftSkillCell.Model, CustomCraftSkillScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<(CustomCraftSkillCell.Model, Transform)> OnClickDetailButton = new();

            public override void Dispose()
            {
                OnClickDetailButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<(CustomCraftSkillCell.Model, Transform)> OnClickDetailButton =>
            Context.OnClickDetailButton;
    }
}

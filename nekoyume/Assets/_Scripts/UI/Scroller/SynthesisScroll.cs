using System;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class SynthesisScroll : RectScroll<SynthesizeModel, SynthesisScroll.ContextModel> // RectScrollDefaultContext
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<Grade> OnClickSelectButton = new();

            public override void Dispose()
            {
                OnClickSelectButton?.Dispose();
                base.Dispose();
            }
        }

        private void Awake()
        {
            ClearContents();
        }

        private void ClearContents()
        {
            foreach (Transform child in cellContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}

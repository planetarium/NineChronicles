using System;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class CollectionScroll : RectScroll<CollectionModel, CollectionScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<CollectionModel> OnClickActiveButton = new Subject<CollectionModel>();
            public readonly Subject<CollectionMaterial> OnClickMaterial = new Subject<CollectionMaterial>();

            public override void Dispose()
            {
                OnClickActiveButton?.Dispose();
                base.Dispose();
            }
        }

        public IObservable<CollectionModel> OnClickActiveButton => Context.OnClickActiveButton;
        public IObservable<CollectionMaterial> OnClickMaterial => Context.OnClickMaterial;

        [SerializeField]
        private TextMeshProUGUI noneText;

        protected override void UpdateContents(IList<CollectionModel> items)
        {
            base.UpdateContents(items);

            noneText.gameObject.SetActive(items.Count == 0);
        }
    }
}

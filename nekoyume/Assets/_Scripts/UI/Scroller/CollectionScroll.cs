using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
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

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine;

        protected override void UpdateContents(IList<CollectionModel> items)
        {
            base.UpdateContents(items);

            noneText.gameObject.SetActive(items.Count == 0);
            AnimateScroller();
        }

        private void AnimateScroller()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
                var rows = GetComponentsInChildren<CollectionCell>(true);
                foreach (var row in rows)
                {
                    row.ShowWithAlpha(true);
                }
            }

            _animationCoroutine = StartCoroutine(CoAnimateScroller());
        }

        private IEnumerator CoAnimateScroller()
        {
            Scroller.Draggable = false;

            yield return null;
            Relayout();

            var rows = GetComponentsInChildren<CollectionCell>();
            var wait = new WaitForSeconds(animationInterval);

            foreach (var row in rows)
            {
                row.HideWithAlpha();
            }

            yield return null;

            foreach (var row in rows)
            {
                row.ShowWithAlpha();
                yield return wait;
            }

            Scroller.Draggable = true;

            _animationCoroutine = null;
        }
    }
}

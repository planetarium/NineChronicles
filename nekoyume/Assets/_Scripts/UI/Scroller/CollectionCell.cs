using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CollectionCell : RectCell<CollectionModel, CollectionScroll.ContextModel>
    {
        [SerializeField] private CollectionStat complete;
        [SerializeField] private CollectionStat incomplete;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton activeButton;
        [SerializeField] private GameObject activeButtonLoadingObject;

        private CollectionModel _itemData;

        private void Awake()
        {
            LoadingHelper.ActivateCollection
                .Subscribe(activeButtonLoadingObject.SetActive)
                .AddTo(gameObject);

            activeButton.OnSubmitSubject
                .Subscribe(_=> Context.OnClickActiveButton.OnNext(_itemData))
                .AddTo(gameObject);
        }

        public override void UpdateContent(CollectionModel itemData)
        {
            _itemData = itemData;

            complete.gameObject.SetActive(false);
            incomplete.gameObject.SetActive(false);

            var cellBackground = itemData.Active ? complete : incomplete;
            cellBackground.Set(itemData);

            var materialCount = itemData.Row.Materials.Count;

            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                collectionItemViews[i].Set(
                    itemData.Materials[i],
                    model => Context.OnClickMaterial.OnNext(model));
            }

            activeButton.SetCondition(() => itemData.CanActivate);
            activeButton.Interactable = !itemData.Active;
        }
    }
}

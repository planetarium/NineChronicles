using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CollectionEffectPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private CollectionEffect collectionEffect;

        [SerializeField]
        private Button goToCollectionButton;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
            };

            goToCollectionButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                GoToCollection();
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            collectionEffect.Set(
                collectionState.Ids.Count,
                collectionSheet.Count,
                collectionState.GetEffects(collectionSheet));
        }

        private void GoToCollection()
        {
            CloseWithOtherWidgets();
            Find<Collection>().Show();
        }
    }
}

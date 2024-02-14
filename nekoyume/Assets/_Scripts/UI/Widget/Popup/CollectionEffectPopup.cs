using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CollectionEffectPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private CollectionEffect collectionEffect;

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
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var models = Find<Collection>().Models;
            var (activeCount, stats) = models.GetEffect();
            collectionEffect.Set(activeCount, models.Count, stats, true);
        }
    }
}

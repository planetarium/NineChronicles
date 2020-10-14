using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CodeReward : Widget
    {
        [SerializeField] private SimpleCountableItemView[] itemViews;
        [SerializeField] private SubmitButton submitButton;
        [SerializeField] private TouchHandler closeTouchHandler;
        [SerializeField] private TouchHandler openTouchHandler;
        [SerializeField] private Animator animator;

        private static readonly int hashAppear = Animator.StringToHash("UICodeReward@Appear");
        private static readonly int hashOpen = Animator.StringToHash("UICodeReward@Open");

        public override void Initialize()
        {
            base.Initialize();

            submitButton.SetSubmitText(
                L10nManager.Localize("UI_RECEIVE"),
                L10nManager.Localize("UI_RECEIVE")
            );

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Close();
            }).AddTo(gameObject);

            closeTouchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                {
                    AudioController.PlayClick();
                    Close();
                }
            }).AddTo(gameObject);

            openTouchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                Next();
            }).AddTo(gameObject);

            animator.Play(hashAppear);

            CloseWidget = null;
        }

        public void Pop(List<(ItemBase, int)> items)
        {
            for (var i = 0; i < itemViews.Length; i++)
            {
                var view = itemViews[i];
                view.gameObject.SetActive(false);
                if (i < items.Count)
                {
                    var (item, count) = items[i];
                    var countableItem = new CountableItem(item, count);
                    view.SetData(countableItem);
                    view.gameObject.SetActive(true);
                }
            }

            base.Show();
        }

        private void Next()
        {
            animator.Play(hashOpen);
        }
    }
}

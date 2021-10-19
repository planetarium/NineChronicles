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
    public class CodeRewardEffector : Widget
    {
        [SerializeField] private TouchHandler openTouchHandler = null;
        [SerializeField] private TouchHandler closeTouchHandler = null;
        [SerializeField] private SubmitButton closeButton = null;

        // todo : 항상 보상으로 받는 아이템이 4개인지 체크해 봐야함. 기존은 4개 였음
        [SerializeField] private SimpleCountableItemView[] itemViews = null;

        private static readonly int AppearHash = Animator.StringToHash("UICodeReward@Appear");
        private static readonly int OpenHash = Animator.StringToHash("UICodeReward@Open");

        protected override void Awake()
        {
            base.Awake();

            openTouchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                Animator.Play(OpenHash);
            }).AddTo(gameObject);

            closeTouchHandler.OnClick.Subscribe(pointerEventData =>
            {
                Close(true);
            }).AddTo(gameObject);

            closeButton.SetSubmitText(L10nManager.Localize("UI_RECEIVE"),
                L10nManager.Localize("UI_RECEIVE"));

            closeButton.OnSubmitClick.Subscribe(_ =>
            {
                Close(true);
            }).AddTo(gameObject);

            CloseWidget = null;
            gameObject.SetActive(false);
        }

        public void Play(IReadOnlyList<(ItemBase, int)> items)
        {
            SetItems(items);
            Show();
            Animator.Play(AppearHash);
        }

        public void PlayRewardSfx()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.Notice);
        }

        private void SetItems(IReadOnlyList<(ItemBase, int)> items)
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
        }
    }
}

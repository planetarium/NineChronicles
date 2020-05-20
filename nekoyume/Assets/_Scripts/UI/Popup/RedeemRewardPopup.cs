using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI
{
    public class RedeemRewardPopup : PopupWidget
    {
        public SimpleCountableItemView[] itemViews;
        public SubmitButton submitButton;
        public TouchHandler touchHandler;

        public override void Initialize()
        {
            base.Initialize();

            submitButton.SetSubmitText(
                LocalizationManager.Localize("UI_CLOSE"),
                LocalizationManager.Localize("UI_CLOSE")
            );

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Close();
            }).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                {
                    AudioController.PlayClick();
                    Close();
                }
            }).AddTo(gameObject);

            CloseWidget = null;
        }
    }
}

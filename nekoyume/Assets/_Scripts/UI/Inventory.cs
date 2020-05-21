using System;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// Fix me.
    /// Status 위젯과 함께 사용할 때에는 해당 위젯 하위에 포함되어야 함.
    /// 지금은 별도의 위젯으로 작동하는데, 이 때문에 위젯 라이프 사이클의 일관성을 잃음.(스스로 닫으면 안 되는 예외 발생)
    /// </summary>
    public class Inventory : XTweenWidget
    {
        public Module.Inventory inventory;
        public Button closeButton;
        public Blur blur;

        private IDisposable _disposableForSelectItem;

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<Status>()?.CloseInventory();
            }).AddTo(gameObject);

            CloseWidget = closeButton.onClick.Invoke;
        }

        #region Widget

        public override void Initialize()
        {
            base.Initialize();
            inventory.SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            inventory.SharedModel.State.Value = ItemType.Equipment;

            if (blur)
            {
                blur.Show();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void SubscribeSelectedItemView(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null ||
                view.RectTransform == tooltip.Target ||
                view.Model?.ItemBase is null)
            {
                tooltip.Close();

                return;
            }

            var subType = view.Model.ItemBase.Value.Data.ItemSubType;
            if (subType == ItemSubType.ApStone)
            {
                tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    DimmedFuncForChargeActionPoint,
                    LocalizationManager.Localize("UI_CHARGE_AP"),
                    _ => ChargeActionPoint((Material) view.Model.ItemBase.Value),
                    _ => inventory.SharedModel.DeselectItemView());
            }
            else
            {
                tooltip.Show(
                    view.RectTransform,
                    view.Model,
                    _ => inventory.SharedModel.DeselectItemView());
            }
        }

        private static void ChargeActionPoint(Material material)
        {
            Notification.Push(Nekoyume.Model.Mail.MailType.System,
                LocalizationManager.Localize("UI_CHARGE_AP"));
            Game.Game.instance.ActionManager.ChargeActionPoint();
            LocalStateModifier.RemoveItem(States.Instance.CurrentAvatarState.address, material.Data.ItemId, 1);
            LocalStateModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                States.Instance.GameConfigState.ActionPointMax);
        }

        private static bool DimmedFuncForChargeActionPoint(CountableItem item)
        {
            if (item is null || item.Count.Value < 1)
            {
                return false;
            }

            return States.Instance.CurrentAvatarState.actionPoint != States.Instance.GameConfigState.ActionPointMax;
        }
    }
}

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SynthesisModule : MonoBehaviour
    {
        // TODO: fix limit count
        private const int LimitSynthesisMaterialCount = 10000;

        [SerializeField]
        private SynthesisMaterialScroll scroll = null!;

        [SerializeField]
        private ConditionalCostButton synthesisButton = null!;

        [SerializeField]
        private ConditionalButton removeAllButton = null!;

        [Header("Texts")]
        [SerializeField]
        private GameObject possibleSynthesisTextObj = null!;

        [SerializeField]
        private GameObject impossibleSynthesisTextObj = null!;

        [SerializeField]
        private TMP_Text numberSynthesisText = null!;

        [SerializeField]
        private TMP_Text successRateText = null!;

        private int _inventoryApStoneCount;

        private ItemSubType _itemSubType;

        private IList<InventoryItem> _selectedItemsForSynthesize = new List<InventoryItem>();

        private bool IsStrong(ItemBase itemBase)
        {
            if (itemBase is Equipment equipment)
            {
                return equipment.level > 0;
            }

            return false;
        }

        #region MonoBehavioir

        private void Awake()
        {
            InitSynthesisButton();
            removeAllButton.OnSubmitSubject.Subscribe(_ =>
            {
                scroll.ClearData();
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            _selectedItemsForSynthesize.Clear();
            scroll.UpdateData(_selectedItemsForSynthesize);
        }

        #endregion MonoBehavioir

        public void UpdateData(IList<InventoryItem> inventoryItems, ItemSubType itemSubType)
        {
            _itemSubType = itemSubType;
            _selectedItemsForSynthesize = inventoryItems;

            var count = inventoryItems.Count;
            var possibleSynthesis = count > 0;

            synthesisButton.Interactable = possibleSynthesis;
            removeAllButton.Interactable = count > 1;

            possibleSynthesisTextObj.SetActive(possibleSynthesis);
            impossibleSynthesisTextObj.SetActive(!possibleSynthesis);

            if (possibleSynthesis)
            {
                // TODO: 연출 작업시 실제 데이터 채워놓겠습니다
                numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", -1);
                successRateText.text = L10nManager.Localize("UI_SYNTHESIZE_SUCCESS_RATE", -1);
            }

            scroll.UpdateData(inventoryItems);
            scroll.RawJumpTo(count - 1);
        }

        #region PushAction

        private void ActionSynthesize(List<ItemBase> equipments)
        {
            if (!equipments.Any() || equipments.Count > LimitSynthesisMaterialCount)
            {
                NcDebug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            CheckSynthesizeStringEquipment(equipments, () =>
                CheckChargeAp(chargeAp => PushAction(equipments, _itemSubType, chargeAp)));
        }

        private void CheckSynthesizeStringEquipment(List<ItemBase> itemBaseList, System.Action callback)
        {
            if (itemBaseList.Exists(IsStrong))
            {
                var system = Widget.Find<IconAndButtonSystem>();
                system.ShowWithTwoButton("UI_WARNING", "UI_SYNTHESIZE_STRONG_CONFIRM");
                system.ConfirmCallback = callback;
            }
            else
            {
                callback();
            }
        }

        private void CheckChargeAp(Action<bool> chargeAp)
        {
            switch (synthesisButton.CurrentState.Value)
            {
                case ConditionalButton.State.Conditional:
                {
                    var paymentPopup = Widget.Find<PaymentPopup>();
                    if (_inventoryApStoneCount > 0)
                    {
                        paymentPopup.ShowCheckPaymentApPotion(GameConfig.ActionCostAP, () => chargeAp(true));
                    }
                    else
                    {
                        paymentPopup.ShowLackApPotion(1);
                    }
                    break;
                }
                case ConditionalButton.State.Normal:
                    chargeAp(false);
                    break;
                case ConditionalButton.State.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PushAction(List<ItemBase> itemBaseList, ItemSubType itemSubType, bool chargeAp)
        {
            StartCoroutine(CoAnimateNPC());

            ActionManager.Instance
                         .Synthesize(itemBaseList, itemSubType, chargeAp)
                         .Subscribe(eval =>
                         {
                             if (eval.Exception == null)
                             {
                                 return;
                             }

                             NcDebug.LogException(eval.Exception.InnerException);
                             OneLineSystem.Push(
                                 MailType.Grinding,
                                 L10nManager.Localize("ERROR_UNKNOWN"),
                                 NotificationCell.NotificationType.Alert);
                         });
            scroll.ClearData();
            AudioController.instance.PlaySfx(AudioController.SfxCode.Heal);
        }

        #endregion PushAction

        #region NPC Animation

        private IEnumerator CoAnimateNPC()
        {
            // TODO
            yield return null;
        }

        #endregion NPC Animation

        #region Init

        private void InitSynthesisButton()
        {
            synthesisButton.SetCost(CostType.ActionPoint, GameConfig.ActionCostAP);

            synthesisButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                var message = scroll.DataCount > 0
                    ? "UI_SYNTHESIZE_SLOT_NOTICE"
                    : "UI_SYNTHESIZE_NOT_EQUIPPED";
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize(message),
                    NotificationCell.NotificationType.Notification);
            }).AddTo(gameObject);

            synthesisButton.OnClickSubject.Subscribe(state =>
            {
                switch (state)
                {
                    case ConditionalButton.State.Normal:
                    case ConditionalButton.State.Conditional:
                        ActionSynthesize(_selectedItemsForSynthesize
                                         .Select(inventoryItem => inventoryItem.ItemBase).ToList());
                        break;
                    case ConditionalButton.State.Disabled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }).AddTo(gameObject);
        }

        #endregion Init
    }
}

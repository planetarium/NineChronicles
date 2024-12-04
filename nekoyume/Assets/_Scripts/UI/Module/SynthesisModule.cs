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
                ClearScrollData();
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            ClearScrollData();
        }

        #endregion MonoBehavioir

        public void UpdateData(IList<InventoryItem> registrationItems, SynthesizeModel model)
        {
            _itemSubType = model.ItemSubType;
            _selectedItemsForSynthesize = registrationItems;

            var synthesisCount = registrationItems.Count / model.RequiredItemCount;
            var possibleSynthesis = synthesisCount > 0;

            synthesisButton.Interactable = possibleSynthesis;
            removeAllButton.Interactable = synthesisCount > 1;

            possibleSynthesisTextObj.SetActive(possibleSynthesis);
            impossibleSynthesisTextObj.SetActive(!possibleSynthesis);

            numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", possibleSynthesis ? synthesisCount : 0);

            var sheet = Game.Game.instance.TableSheets.SynthesizeSheet;
            var row = sheet.Values.FirstOrDefault(row => row.GradeId == (int)model.Grade);
            var succeedRateInt = row?.RequiredCountDict[_itemSubType].SucceedRate ?? 0;
            successRateText.text = L10nManager.Localize("UI_SYNTHESIZE_SUCCESS_RATE", possibleSynthesis ? succeedRateInt * 0.01f : 0);

            scroll.UpdateData(registrationItems);
            scroll.RawJumpTo(registrationItems.Count - 1);
        }

        private void ClearScrollData()
        {
            _selectedItemsForSynthesize.Clear();
            scroll.UpdateData(_selectedItemsForSynthesize);

            synthesisButton.Interactable = false;
            removeAllButton.Interactable = false;

            possibleSynthesisTextObj.SetActive(false);
            impossibleSynthesisTextObj.SetActive(true);

            numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", 0);
            successRateText.text = L10nManager.Localize("UI_SYNTHESIZE_SUCCESS_RATE", 0);
        }

        #region PushAction

        private void ActionSynthesize(List<ItemBase> itemBaseList)
        {
            if (!itemBaseList.Any() || itemBaseList.Count > LimitSynthesisMaterialCount)
            {
                NcDebug.LogWarning($"Invalid selected items count. count : {itemBaseList.Count}");
                return;
            }

            CheckSynthesizeStringEquipment(itemBaseList, () =>
                CheckChargeAp(chargeAp => PushAction(itemBaseList, _itemSubType, chargeAp)));
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
            ClearScrollData();
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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SynthesisModule : MonoBehaviour
    {
        private readonly int isActiveAnimHash = Animator.StringToHash("IsActive");

        private readonly List<IDisposable> _disposables = new();

        [SerializeField]
        private SynthesisMaterialScroll scroll = null!;

        [SerializeField]
        private ConditionalCostButton synthesisButton = null!;

        [SerializeField]
        private ConditionalButton removeAllButton = null!;

        [SerializeField]
        private GameObject buttonBlockerOnSynthesis = null!;

        [Header("Animation")]
        [SerializeField]
        private Animator pendantAnimator = null!;

        [SerializeField]
        private Image pendantItemIcon = null!;

        [SerializeField]
        private Image itemBackground = null!;

        [SerializeField]
        private float itemIconAnimInterval = 0.7f;

        [SerializeField]
        private GameObject actionLoadingIndicator = null!;

        [Header("Texts")]
        [SerializeField]
        private GameObject possibleSynthesisTextObj = null!;

        [SerializeField]
        private GameObject impossibleSynthesisTextObj = null!;

        [SerializeField]
        private TMP_Text numberSynthesisText = null!;

        [SerializeField]
        private TMP_Text successRateText = null!;

        private IList<InventoryItem> _selectedItemsForSynthesize = new List<InventoryItem>();

        private ItemSubType _itemSubType;
        private CancellationTokenSource? _expectationsItemIconCts;

        public bool PossibleSynthesis => _selectedItemsForSynthesize.Count > 0;

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

            ReactiveAvatarState.ObservableActionPoint
                .Subscribe(_ => synthesisButton.UpdateObjects())
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

#endregion MonoBehavioir

        public void UpdateData(IList<InventoryItem> registrationItems, SynthesizeModel model)
        {
            _itemSubType = model.ItemSubType;
            _selectedItemsForSynthesize = registrationItems;

            var synthesisCount = registrationItems.Count / model.RequiredItemCount;
            var possibleSynthesis = synthesisCount > 0;

            SetSynthesisButtonState(possibleSynthesis);

            numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", possibleSynthesis ? synthesisCount : 0);

            var sheet = Game.Game.instance.TableSheets.SynthesizeSheet;
            var row = sheet.Values.FirstOrDefault(row => row.GradeId == (int)model.Grade);
            var succeedRateInt = row?.RequiredCountDict[_itemSubType].SucceedRate ?? 0;
            successRateText.text = L10nManager.Localize("UI_SYNTHESIZE_SUCCESS_RATE", possibleSynthesis ? succeedRateInt * 0.01f : 0);

            scroll.UpdateData(registrationItems);
            scroll.RawJumpTo(registrationItems.Count - 1);

            ClearExpectationsItemIconCts();
            _expectationsItemIconCts = new CancellationTokenSource();
            ShowExpectationsItemIcon(model.Grade, model.ItemSubType, _expectationsItemIconCts).Forget();
        }

        private void ClearScrollData()
        {
            _selectedItemsForSynthesize.Clear();
            scroll.UpdateData(_selectedItemsForSynthesize);
            SetSynthesisButtonState(false);

            ClearExpectationsItemIconCts();

            numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", 0);
            successRateText.text = L10nManager.Localize("UI_SYNTHESIZE_SUCCESS_RATE", 0);
        }

        private void SetSynthesisButtonState(bool possibleSynthesis)
        {
            synthesisButton.Interactable = possibleSynthesis;
            removeAllButton.Interactable = possibleSynthesis;
            pendantAnimator.SetBool(isActiveAnimHash, possibleSynthesis);

            possibleSynthesisTextObj.SetActive(possibleSynthesis);
            impossibleSynthesisTextObj.SetActive(!possibleSynthesis);
        }

        private async UniTask ShowExpectationsItemIcon(Grade grade, ItemSubType itemSubType, CancellationTokenSource cts)
        {
            var pool = Synthesis.GetSynthesizeResultPool(grade, itemSubType);
            if (pool == null || pool.Count == 0)
            {
                NcDebug.LogError($"[{nameof(SynthesisModule)}] pool is empty.");
                return;
            }

            if (cts.IsCancellationRequested)
            {
                NcDebug.LogWarning($"[{nameof(SynthesisModule)}] cts is canceled.");
                return;
            }

            while (!cts.IsCancellationRequested)
            {
                foreach (var poolItem in pool)
                {
                    pendantItemIcon.sprite = SpriteHelper.GetItemIcon(poolItem.Item1);
                    itemBackground.sprite = SpriteHelper.GetItemBackground((int)poolItem.Item2);
                    var isCancelled = await UniTask.Delay(TimeSpan.FromSeconds(itemIconAnimInterval), cancellationToken: cts.Token).SuppressCancellationThrow();
                    if (isCancelled)
                    {
                        return;
                    }
                }
            }
        }

        private void ClearExpectationsItemIconCts()
        {
            _expectationsItemIconCts?.Cancel();
            _expectationsItemIconCts?.Dispose();
            _expectationsItemIconCts = null;
        }

        #region PushAction

        private void ActionSynthesize(List<ItemBase> itemBaseList)
        {
            if (!itemBaseList.Any())
            {
                NcDebug.LogWarning($"[{nameof(SynthesisModule)}] itemBaseList is empty.");
                return;
            }

            var grade = itemBaseList[0] switch
            {
                Equipment equipment => (Grade)equipment.Grade,
                Costume costume => (Grade)costume.Grade,
                _ => throw new ArgumentException($"Invalid item type: {itemBaseList[0].GetType()}"),
            };

            CheckChargeAp(chargeAp => PushAction(itemBaseList, chargeAp, grade, _itemSubType));
        }

        private void CheckChargeAp(Action<bool> chargeAp)
        {
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            var inventoryApStoneCount = inventory.GetUsableItemCount(
                (int)CostType.ApPotion,
                Game.Game.instance.Agent?.BlockIndex ?? -1);

            switch (synthesisButton.CurrentState.Value)
            {
                case ConditionalButton.State.Conditional:
                {
                    var paymentPopup = Widget.Find<PaymentPopup>();
                    if (inventoryApStoneCount > 0)
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

        private void PushAction(List<ItemBase> itemBaseList, bool chargeAp, Grade grade, ItemSubType itemSubType)
        {
            if (itemBaseList.Count == 0)
            {
                NcDebug.LogWarning($"[{nameof(SynthesisModule)}] itemBaseList is empty.");
                return;
            }

            var loadingScreen = Widget.Find<SynthesizeLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetCloseAction(null);

            var pool = Synthesis.GetSynthesizeResultPool((Grade)itemBaseList[0].Grade, itemBaseList[0].ItemSubType);
            loadingScreen.AnimateNpc(pool);

            ActionManager.Instance
                         .Synthesize(itemBaseList, chargeAp, grade, itemSubType)
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
            SetOnActionState(true);
        }

        public void SetOnActionState(bool active)
        {
            actionLoadingIndicator.SetActive(active);
            buttonBlockerOnSynthesis.SetActive(active);
        }

        #endregion PushAction

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

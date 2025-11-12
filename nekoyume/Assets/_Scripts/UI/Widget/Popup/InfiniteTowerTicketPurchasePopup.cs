using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using UnityEngine.UI;
using Nekoyume.Model.Item;
using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.Helper;

namespace Nekoyume.UI
{
    using UniRx;

    public class InfiniteTowerTicketPurchasePopup : PopupWidget
    {
        [SerializeField] private CostIconDataScriptableObject costIconData;
        [SerializeField] private Image ticketIcon;
        [SerializeField] private ConditionalCostButton ncgPurchaseButton;
        [SerializeField] private ConditionalCostButton materialPurchaseButton;
        [SerializeField] private TextMeshProUGUI purchaseText;
        [SerializeField] private TextMeshProUGUI purchaseCountText;

        private int _floorId;
        private System.Action _onConfirmNcg;
        private System.Action _onConfirmMaterial;
        private readonly List<IDisposable> _disposables = new();
        private int? _materialCostId;
        private int? _materialCostCount;

        protected override void Awake()
        {
            base.Awake();

            ncgPurchaseButton.OnClickSubject.Subscribe(_ =>
            {
                if (ncgPurchaseButton.CurrentState.Value == ConditionalButton.State.Conditional)
                {
                    Close(true);
                }
            }).AddTo(gameObject);

            materialPurchaseButton.OnClickSubject.Subscribe(_ =>
            {
                if (materialPurchaseButton.CurrentState.Value == ConditionalButton.State.Conditional)
                {
                    Close(true);
                }
            }).AddTo(gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }

        public void Show(
            int floorId,
            int? ncgCost,
            int? materialCostId,
            int? materialCostCount,
            int purchasedCount,
            System.Action onConfirmNcg,
            System.Action onConfirmMaterial,
            System.Action onClose = null,
            bool ignoreShowAnimation = false)
        {
            _floorId = floorId;
            _onConfirmNcg = onConfirmNcg;
            _onConfirmMaterial = onConfirmMaterial;

            // 기존 구독 해제
            _disposables.DisposeAllAndClear();

            ticketIcon.overrideSprite = costIconData.GetIcon(CostType.InfiniteTowerTicket);
            var purchaseMessage = L10nManager.Localize("UI_TICKET_PURCHASE_LIMIT");
            purchaseCountText.gameObject.SetActive(true);
            purchaseCountText.text = $"<size=150%>{purchasedCount}/∞</size>";
            purchaseCountText.color = Palette.GetColor(EnumType.ColorType.TextElement06);
            purchaseText.text = purchaseMessage;
            purchaseText.color = Palette.GetColor(EnumType.ColorType.TextElement06);

            // 새로 구독
            ncgPurchaseButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                NcDebug.Log($"Purchasing InfiniteTower ticket for floor {_floorId} with NCG {ncgCost}");
                _onConfirmNcg?.Invoke();
            }).AddTo(_disposables);

            materialPurchaseButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                NcDebug.Log($"Purchasing InfiniteTower ticket for floor {_floorId} with Material {materialCostId} x {materialCostCount}");
                _onConfirmMaterial?.Invoke();
            }).AddTo(_disposables);

            // NCG 버튼 설정
            bool hasNcgOption = ncgCost.HasValue;
            if (hasNcgOption)
            {
                ncgPurchaseButton.SetCost(CostType.NCG, ncgCost.Value);
                ncgPurchaseButton.SetText(L10nManager.Localize("UI_BUY"));
                ncgPurchaseButton.UpdateObjects();
            }
            else
            {
                ncgPurchaseButton.SetState(ConditionalButton.State.Disabled);
            }

            // Material 버튼 설정
            bool hasMaterialOption = materialCostId.HasValue && materialCostCount.HasValue;
            _materialCostId = materialCostId;
            _materialCostCount = materialCostCount;

            if (hasMaterialOption)
            {
                // Material CostType 결정
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                CostType materialCostType = CostType.None;
                string materialName = "";

                if (materialSheet.TryGetValue(materialCostId.Value, out var materialRow))
                {
                    materialName = materialRow.GetLocalizedName(useElementalIcon: false);

                    // Material ID를 CostType enum 값과 직접 비교
                    // CostType enum: GoldDust=600201, RubyDust=600202, EmeraldDust=600203, SapphireDust=600206, SilverDust=800201
                    materialCostType = materialCostId.Value switch
                    {
                        (int)CostType.GoldDust => CostType.GoldDust,
                        (int)CostType.SilverDust => CostType.SilverDust,
                        (int)CostType.RubyDust => CostType.RubyDust,
                        (int)CostType.EmeraldDust => CostType.EmeraldDust,
                        (int)CostType.SapphireDust => CostType.SapphireDust,
                        _ => CostType.None,
                    };
                }
                else
                {
                    materialName = $"Material {materialCostId.Value}";
                }

                if (materialCostType != CostType.None)
                {
                    // 특정 CostType으로 매핑 가능한 경우 (ConditionalCostButton이 자동으로 체크)
                    materialPurchaseButton.SetCost(materialCostType, materialCostCount.Value);
                    NcDebug.Log($"[InfiniteTowerTicketPurchasePopup] Material {materialCostId.Value} mapped to CostType {materialCostType}");
                    // 버튼 텍스트는 로컬라이즈된 텍스트만 사용 (cost는 SetCost로 별도 표시)
                    materialPurchaseButton.SetText(L10nManager.Localize("UI_BUY"));
                }
                else
                {
                    // 일반 Material인 경우 CostType.None으로 설정하고 커스텀 체크 추가
                    materialPurchaseButton.SetCost(CostType.None, materialCostCount.Value);

                    // Material 인벤토리 체크를 위한 커스텀 조건 추가
                    materialPurchaseButton.SetCondition(() =>
                    {
                        var inventory = States.Instance.CurrentAvatarState?.inventory;
                        if (inventory == null)
                        {
                            NcDebug.Log($"[InfiniteTowerTicketPurchasePopup] Inventory is null");
                            return false;
                        }
                        var materialCount = inventory.GetMaterialCount(materialCostId.Value);
                        var hasEnough = materialCount >= materialCostCount.Value;
                        NcDebug.Log($"[InfiniteTowerTicketPurchasePopup] Material {materialCostId.Value} check: count={materialCount}, required={materialCostCount.Value}, hasEnough={hasEnough}");
                        return hasEnough;
                    });
                    // 버튼 텍스트는 재료 이름과 개수 포함
                    materialPurchaseButton.SetText(L10nManager.Localize("UI_INFINITETOWER_TICKET_PURCHASE_MATERIAL_BTN", materialName, materialCostCount.Value));
                }
            }
            else
            {
                materialPurchaseButton.SetState(ConditionalButton.State.Disabled);
            }

            // 두 옵션이 모두 없는 경우
            if (!hasNcgOption && !hasMaterialOption)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFINITETOWER_TICKET_COST_NOT_CONFIGURED"),
                    NotificationCell.NotificationType.Alert);
                Close();
                return;
            }

            // Close 콜백 설정
            if (onClose != null)
            {
                Show(onClose, ignoreShowAnimation);
            }
            else
            {
                base.Show(ignoreShowAnimation);
            }
        }
    }
}

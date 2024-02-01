using System;
using System.Linq;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UniRx;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Tween;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class RecipeCellView : MonoBehaviour
    {
        protected static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f);

        [SerializeField]
        protected Button button;

        [SerializeField]
        protected Image panelImageLeft;

        [SerializeField]
        protected Image panelImageRight;

        [SerializeField]
        protected Image backgroundImage;

        [SerializeField]
        protected Image[] elementalTypeImages;

        [SerializeField]
        protected TextMeshProUGUI titleText;

        [SerializeField]
        protected TextMeshProUGUI optionText;

        [SerializeField]
        protected SimpleCountableItemView itemView;

        [SerializeField]
        protected GameObject lockParent;

        [SerializeField]
        protected TextMeshProUGUI unlockConditionText;

        [SerializeField]
        protected CanvasGroup canvasGroup;

        [SerializeField]
        protected Image hasNotificationImage;

        [SerializeField]
        protected LockChainJitterVFX lockVFX = null;

        public RectTransformShakeTweener shakeTweener = null;
        public TransformLocalScaleTweener scaleTweener = null;

        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        public readonly Subject<RecipeCellView> OnClick =
            new Subject<RecipeCellView>();

        public bool IsLocked => lockParent.activeSelf;
        public ItemSubType ItemSubType { get; protected set; }
        public ElementalType ElementalType { get; protected set; }
        public StatType StatType { get; protected set; }
        public EquipmentItemRecipeSheet.Row EquipmentRowData { get; private set; }
        public ConsumableItemRecipeSheet.Row ConsumableRowData { get; private set; }
        public int UnlockStage { get; private set; }

        public SimpleCountableItemView ItemView => itemView;

        public bool tempLocked = false;

        public bool Visible
        {
            get => Mathf.Approximately(canvasGroup.alpha, 1f);
            set => canvasGroup.alpha = value ? 1f : 0f;
        }

        private void Awake()
        {
            button.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    if (IsLocked && !tempLocked)
                    {
                        return;
                    }

                    OnClick.OnNext(this);
                })
                .AddTo(gameObject);

            if (hasNotificationImage)
                HasNotification.SubscribeTo(hasNotificationImage)
                    .AddTo(gameObject);
        }

        private void OnDestroy()
        {
            OnClick.Dispose();
        }

        public void Show()
        {
            Visible = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            shakeTweener?.KillTween();
        }

        protected void Set(ItemUsable itemUsable)
        {
            ItemSubType = itemUsable.ItemSubType;
            ElementalType = itemUsable.ElementalType;

            titleText.text = itemUsable.GetLocalizedNonColoredName();

            var item = new CountableItem(itemUsable, 1);
            itemView.SetData(item);

            var sprite = ElementalType.GetSprite();
            var grade = itemUsable.Grade;

            for (var i = 0; i < elementalTypeImages.Length; ++i)
            {
                if (sprite is null || i >= grade)
                {
                    elementalTypeImages[i].gameObject.SetActive(false);
                    continue;
                }

                elementalTypeImages[i].sprite = sprite;
                elementalTypeImages[i].gameObject.SetActive(true);
            }

            SetCellViewLocked(false);
            SetDimmed(false);
        }

        public void SetInteractable(bool value)
        {
            button.interactable = value;
        }

        protected void SetCellViewLocked(bool value)
        {
            lockParent.SetActive(value);
            itemView.gameObject.SetActive(!value);
            titleText.enabled = !value;
            optionText.enabled = !value;

            foreach (var icon in elementalTypeImages)
            {
                icon.enabled = !value;
            }

            SetPanelDimmed(value);
        }

        protected void SetDimmed(bool value)
        {
            var color = value ? DisabledColor : Color.white;
            titleText.color = itemView.Model.ItemBase.Value.GetItemGradeColor() * color;
            optionText.color = color;
            itemView.Model.Dimmed.Value = value;

            foreach (var icon in elementalTypeImages)
            {
                icon.color = value ? DisabledColor : Color.white;
            }

            SetPanelDimmed(value);
        }

        protected void SetPanelDimmed(bool value)
        {
            var color = value ? DisabledColor : Color.white;
            panelImageLeft.color = color;
            panelImageRight.color = color;
            backgroundImage.color = color;
        }

        protected void SetLocked(bool value, int unlockStage)
        {
            // TODO: 나중에 해금 시스템이 분리되면 아래의 해금 조건 텍스트를 얻는 로직을 옮겨서 반복을 없애야 좋겠다.
            if (value)
            {
                unlockConditionText.enabled = true;

                if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var stageId))
                {
                    var diff = unlockStage - stageId;
                    if (diff > 50)
                    {
                        unlockConditionText.text = string.Format(
                            L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            "???");
                    }
                    else
                    {
                        if (diff <= 0 && tempLocked)
                        {
                            lockVFX.Play();
                            shakeTweener.PlayLoop();
                        }
                        unlockConditionText.text = string.Format(
                            L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            unlockStage.ToString());
                    }
                }
                else
                {
                    unlockConditionText.text = string.Format(
                        L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }
            }
            else
            {
                unlockConditionText.enabled = false;
            }

            SetCellViewLocked(value);
        }

        public void Set(EquipmentItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
            {
                return;
            }

            var equipmentSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            if (!equipmentSheet.TryGetValue(recipeRow.ResultEquipmentId, out var row))
            {
                return;
            }

            EquipmentRowData = recipeRow;

            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(equipment);

            StatType = equipment.UniqueStatType;
            optionText.text = equipment.StatsMap.GetBaseStat(StatType).ToString();
            SetLocked(false, EquipmentRowData.UnlockStage);
        }

        public void Set(AvatarState avatarState, bool? hasNotification = false, bool isFirstOpen = false)
        {
            if (!isFirstOpen)
            {
                StopLockEffect();
            }

            if (EquipmentRowData is null)
            {
                return;
            }

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(EquipmentRowData.UnlockStage))
            {
                HasNotification.Value = false;
                SetLocked(true, EquipmentRowData.UnlockStage);
                tempLocked = false;
                return;
            }

            if (hasNotification.HasValue)
            {
                HasNotification.Value = hasNotification.Value;
            }

            tempLocked = isFirstOpen;

            SetLocked(isFirstOpen, EquipmentRowData.UnlockStage);

            if (tempLocked)
            {
                return;
            }

            // 메인 재료 검사.
            var inventory = avatarState.inventory;
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            if (materialSheet.TryGetValue(EquipmentRowData.MaterialId, out var materialRow) &&
                inventory.TryGetFungibleItems(materialRow.ItemId, out var outFungibleItems) &&
                outFungibleItems.Sum(e => e.count) >= EquipmentRowData.MaterialCount)
            {
                // 서브 재료 검사.
                if (EquipmentRowData.SubRecipeIds.Any())
                {
                    var subSheet = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet;
                    var shouldDimmed = false;
                    foreach (var subRow in EquipmentRowData.SubRecipeIds
                        .Select(subRecipeId =>
                            subSheet.TryGetValue(subRecipeId, out var subRow) ? subRow : null)
                        .Where(item => !(item is null)))
                    {
                        shouldDimmed = false;
                        foreach (var info in subRow.Materials)
                        {
                            if (materialSheet.TryGetValue(info.Id, out materialRow) &&
                                inventory.TryGetFungibleItems(materialRow.ItemId, out outFungibleItems) &&
                                outFungibleItems.Sum(e => e.count) >= info.Count)
                            {
                                continue;
                            }

                            shouldDimmed = true;
                            break;
                        }

                        if (!shouldDimmed)
                        {
                            break;
                        }
                    }

                    SetDimmed(shouldDimmed);
                }
                else
                {
                    SetDimmed(false);
                }
            }
            else
            {
                SetDimmed(true);
            }
        }

        public void Set(ConsumableItemRecipeSheet.Row recipeRow)
        {
            StopLockEffect();
            HasNotification.Value = false;
            if (recipeRow is null)
                return;

            UnlockStage = Game.LiveAsset.GameConfig.RequiredStage.CraftConsumable;
            var sheet = Game.Game.instance.TableSheets.ConsumableItemSheet;
            if (!sheet.TryGetValue(recipeRow.ResultConsumableItemId, out var row))
                return;

            ConsumableRowData = recipeRow;

            var consumable = (Consumable)ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(consumable);

            StatType = consumable.MainStat;

            var optionString = $"{consumable.MainStat} +{consumable.Stats.First(stat => stat.StatType == consumable.MainStat).TotalValueAsLong}";
            optionText.text = optionString;
            SetLocked(false, UnlockStage);
        }

        public void Set(AvatarState avatarState)
        {
            if (ConsumableRowData is null)
                return;

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(UnlockStage))
            {
                SetLocked(true, UnlockStage);
                return;
            }

            SetLocked(false, UnlockStage);

            //재료 검사.
            var inventory = avatarState.inventory;
            SetDimmed(!ConsumableRowData.Materials.All(info => inventory.HasItem(info.Id, info.Count)));
        }

        private void StopLockEffect()
        {
            shakeTweener?.KillTween();
            lockVFX?.Stop();
        }
    }
}

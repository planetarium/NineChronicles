using System;
using Assets.SimpleLocalization;
using Nekoyume.Game.VFX;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipeView : EquipmentOptionView
    {
        [SerializeField]
        private TextMeshProUGUI unlockConditionText = null;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private GameObject lockParent = null;

        [SerializeField]
        private GameObject header = null;

        [SerializeField]
        private GameObject options = null;

        [SerializeField]
        protected RecipeClickVFX recipeClickVFX = null;

        [SerializeField]
        protected Image hasNotificationImage = null;

        private bool _tempLocked = false;

        private (int parentItemId, int index) _parentInfo;

        protected readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        public EquipmentItemSubRecipeSheet.Row rowData;

        public readonly Subject<Unit> OnClick = new Subject<Unit>();

        public readonly Subject<EquipmentOptionRecipeView> OnClickVFXCompleted =
            new Subject<EquipmentOptionRecipeView>();

        private bool IsLocked => lockParent.activeSelf;
        private bool NotEnoughMaterials { get; set; } = true;

        private void Awake()
        {
            recipeClickVFX.OnTerminated = () => OnClickVFXCompleted.OnNext(this);

            button.OnClickAsObservable().Subscribe(_ =>
            {
                if (IsLocked && !_tempLocked)
                {
                    return;
                }

                if (_tempLocked)
                {
                    var avatarState = Game.Game.instance.States.CurrentAvatarState;
                    var combination = Widget.Find<Combination>();
                    combination.RecipeVFXSkipMap[_parentInfo.parentItemId][_parentInfo.index] = rowData.Id;
                    combination.SaveRecipeVFXSkipMap();
                    Set(avatarState, null, false);
                    return;
                }

                if (NotEnoughMaterials)
                {
                    return;
                }

                OnClick.OnNext(Unit.Default);
                recipeClickVFX.Play();
            }).AddTo(gameObject);

            if (hasNotificationImage)
                HasNotification.SubscribeTo(hasNotificationImage)
                    .AddTo(gameObject);
        }

        private void OnDisable()
        {
            recipeClickVFX.Stop();
        }

        private void OnDestroy()
        {
            OnClickVFXCompleted.Dispose();
        }

        public void Show(
            string recipeName,
            int subRecipeId,
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            bool checkInventory,
            (int parentItemId, int index)? parentInfo = null
        )
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet.TryGetValue(subRecipeId,
                out rowData))
            {
                requiredItemRecipeView.SetData(baseMaterialInfo, rowData.Materials,
                    checkInventory);
            }
            else
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                Hide();
                return;
            }

            if (parentInfo.HasValue)
            {
                _parentInfo = parentInfo.Value;
            }

            SetLocked(false);
            Show(recipeName, subRecipeId);
        }

        public void Set(AvatarState avatarState, bool? hasNotification = false, bool tempLocked = false)
        {
            if (rowData is null)
            {
                return;
            }

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(rowData.UnlockStage))
            {
                SetLocked(true);
                return;
            }

            if (hasNotification.HasValue)
                HasNotification.Value = hasNotification.Value;

            _tempLocked = tempLocked;
            SetLocked(tempLocked);

            if (tempLocked)
                return;

            // 재료 검사.
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var inventory = avatarState.inventory;
            var shouldDimmed = false;
            foreach (var info in rowData.Materials)
            {
                if (materialSheet.TryGetValue(info.Id, out var materialRow) &&
                    inventory.TryGetMaterial(materialRow.ItemId, out var fungibleItem) &&
                    fungibleItem.count >= info.Count)
                {
                    continue;
                }

                shouldDimmed = true;
                break;
            }

            SetDimmed(shouldDimmed);
        }

        public void ShowLocked()
        {
            SetLocked(true);
            Show();
        }

        private void SetLocked(bool value)
        {
            // TODO: 나중에 해금 시스템이 분리되면 아래의 해금 조건 텍스트를 얻는 로직을 옮겨서 반복을 없애야 좋겠다.
            if (value)
            {
                unlockConditionText.enabled = true;

                if (rowData is null)
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }

                if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var stageId))
                {
                    var diff = rowData.UnlockStage - stageId;
                    if (diff > 50)
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            "???");
                    }
                    else
                    {
                        unlockConditionText.text = string.Format(
                            LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                            rowData.UnlockStage.ToString());
                    }
                }
                else
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }
            }
            else
            {
                unlockConditionText.enabled = false;
            }

            lockParent.SetActive(value);
            header.SetActive(!value);
            options.SetActive(!value);
            requiredItemRecipeView.gameObject.SetActive(!value);
            SetPanelDimmed(value);
        }

        public override void SetDimmed(bool value)
        {
            base.SetDimmed(value);
            NotEnoughMaterials = value;
        }
    }
}

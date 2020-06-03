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

        private EquipmentItemSubRecipeSheet.Row _rowData;

        public readonly Subject<EquipmentOptionRecipeView> OnClick =
            new Subject<EquipmentOptionRecipeView>();

        private bool IsLocked => lockParent.activeSelf;
        private bool NotEnoughMaterials { get; set; } = true;

        private void Awake()
        {
            recipeClickVFX.OnTerminated = () => OnClick.OnNext(this);

            button.OnClickAsObservable().Subscribe(_ =>
            {
                if (IsLocked || NotEnoughMaterials)
                {
                    return;
                }

                recipeClickVFX.Play();
            }).AddTo(gameObject);
        }

        private void OnDisable()
        {
            recipeClickVFX.Stop();
        }

        private void OnDestroy()
        {
            OnClick.Dispose();
        }

        public void Show(
            string recipeName,
            int subRecipeId,
            EquipmentItemSubRecipeSheet.MaterialInfo baseMaterialInfo,
            bool checkInventory = true
        )
        {
            if (Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheet.TryGetValue(subRecipeId,
                out _rowData))
            {
                requiredItemRecipeView.SetData(baseMaterialInfo, _rowData.Materials,
                    checkInventory);
            }
            else
            {
                Debug.LogWarning($"SubRecipe ID not found : {subRecipeId}");
                Hide();
                return;
            }

            SetLocked(false);
            Show(recipeName, subRecipeId);
        }

        public void Set(AvatarState avatarState)
        {
            if (_rowData is null)
            {
                return;
            }

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(_rowData.UnlockStage))
            {
                SetLocked(true);
                return;
            }

            // 재료 검사.
            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var inventory = avatarState.inventory;
            var shouldDimmed = false;
            foreach (var info in _rowData.Materials)
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

                if (_rowData is null)
                {
                    unlockConditionText.text = string.Format(
                        LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                        "???");
                }

                if (States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(
                    out var stageId))
                {
                    var diff = _rowData.UnlockStage - stageId;
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
                            _rowData.UnlockStage.ToString());
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

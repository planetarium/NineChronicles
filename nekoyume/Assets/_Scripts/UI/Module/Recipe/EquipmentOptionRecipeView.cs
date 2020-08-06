using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentOptionRecipeView : EquipmentOptionView
    {
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
        protected LockChainJitterVFX lockVFX = null;

        [SerializeField]
        protected Image hasNotificationImage = null;

        public RectTransformShakeTweener shakeTweener = null;
        public TransformLocalScaleTweener scaleTweener = null;

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
                scaleTweener.PlayTween();

                if (_tempLocked)
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.UnlockRecipe);
                    var avatarState = Game.Game.instance.States.CurrentAvatarState;
                    var combination = Widget.Find<Combination>();
                    combination.RecipeVFXSkipMap[_parentInfo.parentItemId][_parentInfo.index] = rowData.Id;
                    combination.SaveRecipeVFXSkipMap();
                    Set(avatarState, null, false);
                    var centerPos = GetComponent<RectTransform>()
                        .GetWorldPositionOfCenter();
                    VFXController.instance.CreateAndChaseCam<ElementalRecipeUnlockVFX>(centerPos);
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

            if (hasNotification.HasValue)
                HasNotification.Value = hasNotification.Value;

            _tempLocked = tempLocked;
            SetLocked(tempLocked);

            if (tempLocked)
            {
                lockVFX?.Play();
                shakeTweener.PlayLoop();
            }
            else
            {
                lockVFX?.Stop();
                shakeTweener.KillTween();
            }

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

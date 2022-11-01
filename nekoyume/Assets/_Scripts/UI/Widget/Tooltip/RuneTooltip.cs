using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SkillView = Nekoyume.UI.Module.SkillView;

namespace Nekoyume.UI
{
    using UniRx;
    public class RuneTooltip : NewVerticalTooltipWidget
    {
        [SerializeField]
        protected Scrollbar scrollbar;

        [SerializeField]
        private ConditionalButton confirmButton;

        [SerializeField]
        private Button enhancementButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private TextMeshProUGUI runeNameText;

        [SerializeField]
        private TextMeshProUGUI currentLevelText;

        [SerializeField]
        private TextMeshProUGUI maxLevelText;

        [SerializeField]
        private TextMeshProUGUI levelLimitText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private List<StatView> statViewList;

        [SerializeField]
        private SkillView skillView;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewDataScriptableObject;

        private System.Action _onEquip;
        private System.Action _onEnhancement;

        private bool _isClickedButtonArea;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = () => Close(true);
            closeButton.onClick.AddListener(() => Close(true));
            enhancementButton.onClick.AddListener(() => Close(true));
            confirmButton.OnClickSubject.Subscribe(state => Close(true)).AddTo(gameObject);
        }

        public void Show(
            InventoryItem item,
            string confirm,
            bool interactable,
            System.Action onEquip,
            System.Action onEnhancement = null,
            RectTransform target = null)
        {
            confirmButton.Interactable = interactable;
            confirmButton.Text = confirm;
            runeNameText.text = L10nManager.Localize($"ITEM_NAME_{item.RuneState.RuneId}");
            currentLevelText.text = $"+{item.RuneState.Level}";

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (runeListSheet.TryGetValue(item.RuneState.RuneId, out var row))
            {
                var data = itemViewDataScriptableObject.GetItemViewData(row.Grade);
                gradeImage.overrideSprite = data.GradeBackground;
                gradeHsv.range = data.GradeHsvRange;
                gradeHsv.hue = data.GradeHsvHue;
                gradeHsv.saturation = data.GradeHsvSaturation;
                gradeHsv.value = data.GradeHsvValue;

                levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", row.RequiredLevel);
                gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{row.Grade}");
            }

            var runeCostSheet = Game.Game.instance.TableSheets.RuneCostSheet;
            if (runeCostSheet.TryGetValue(item.RuneState.RuneId, out var costRow))
            {
                maxLevelText.text = $"/{costRow.Cost.Count}";
            }

            if (RuneFrontHelper.TryGetRuneIcon(item.RuneState.RuneId, out var icon))
            {
                runeImage.sprite = icon;
            }

            _onEquip = onEquip;
            _onEnhancement = onEnhancement;

            scrollbar.value = 1f;
            UpdatePosition(target);
            base.Show();
        }

        private void UpdatePosition(RectTransform target)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.SetAnchorAndPivot(AnchorPresetType.TopLeft, PivotPresetType.TopLeft);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)verticalLayoutGroup.transform);
            if (target)
            {
                panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
            }
            else
            {
                panel.SetAnchor(AnchorPresetType.MiddleCenter);
                panel.anchoredPosition =
                    new Vector2(-(panel.sizeDelta.x / 2), panel.sizeDelta.y / 2);
            }
            panel.MoveInsideOfParent(MarginFromParent);

            if (!(target is null) && panel.position.x - target.position.x < 0)
            {
                panel.SetAnchorAndPivot(AnchorPresetType.TopRight, PivotPresetType.TopRight);
                panel.MoveToRelatedPosition(target, TargetPivotPresetType.ReverseX(),
                    DefaultOffsetFromTarget.ReverseX());
                UpdateAnchoredPosition(target);
            }
        }
    }
}

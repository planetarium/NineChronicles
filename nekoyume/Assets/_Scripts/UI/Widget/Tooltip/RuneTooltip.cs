using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
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
        private TextMeshProUGUI subTypeText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private Image spacerImage;

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

        [SerializeField]
        private Image adventure;

        [SerializeField]
        private Image arena;

        [SerializeField]
        private Image raid;

        private System.Action _onConfirm;
        private System.Action _onEnhancement;
        private System.Action _onClose;

        private bool _isPointerOnScrollArea;
        private bool _isClickedButtonArea;

        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = () => Close(true);
            SubmitWidget = () =>
            {
                _onConfirm?.Invoke();
                Close();
            };
            enhancementButton.onClick.AddListener(() =>
            {
                _onEnhancement?.Invoke();
                Close(true);
            });
            confirmButton.OnClickSubject.Subscribe(state =>
            {
                _onConfirm?.Invoke();
                Close(true);
            }).AddTo(gameObject);

            confirmButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("UI_MESSAGE_CAN_NOT_EQUIPPED"),
                    NotificationCell.NotificationType.Alert);
            })
            .AddTo(gameObject);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            base.Close(ignoreCloseAnimation);
        }

         public void ShowForDisplay(RuneState runeState)
        {
            confirmButton.gameObject.SetActive(false);
            enhancementButton.gameObject.SetActive(false);
            runeNameText.text = L10nManager.Localize($"RUNE_NAME_{runeState.RuneId}");
            currentLevelText.text = $"+{runeState.Level}";

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (runeListSheet.TryGetValue(runeState.RuneId, out var row))
            {
                levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", row.RequiredLevel);
                UpdateGrade(row);
                UpdateAreaIcon((RuneUsePlace)row.UsePlace);
            }

            var runeCostSheet = Game.Game.instance.TableSheets.RuneCostSheet;
            if (runeCostSheet.TryGetValue(runeState.RuneId, out var costRow))
            {
                maxLevelText.text = $"/{costRow.Cost.Count}";
            }

            if (RuneFrontHelper.TryGetRuneIcon(runeState.RuneId, out var icon))
            {
                runeImage.sprite = icon;
            }

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
            {
                return;
            }

            if (optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
            {
                if (option.SkillId != 0)
                {
                    var name = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                    var currentValueString = RuneFrontHelper.GetRuneValueString(option);
                    var desc = L10nManager.Localize(
                        $"SKILL_DESCRIPTION_{option.SkillId}", option.SkillChance, option.BuffDuration, currentValueString);
                    var cooldown = $"{L10nManager.Localize($"UI_COOLDOWN")} : {option.SkillCooldown}";
                    skillView.Show(name, desc, cooldown);
                }
                else
                {
                    skillView.Hide();
                }

                foreach (var statView in statViewList)
                {
                    statView.gameObject.SetActive(false);
                }

                for (var i = 0; i < option.Stats.Count; i++)
                {
                    var (statMap, _) = option.Stats[i];
                    statViewList[i].gameObject.SetActive(true);
                    statViewList[i].Show(statMap.StatType, statMap.TotalValueAsLong, true);
                }

                cpText.text = $"<size=80%>CP</size> {option.Cp}";
            }

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(confirmButton.gameObject));
        }

        public void Show(
            InventoryItem item,
            string confirm,
            bool interactable,
            System.Action onConfirm,
            System.Action onEnhancement = null,
            System.Action onClose = null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.Interactable = interactable;
            enhancementButton.gameObject.SetActive(true);
            enhancementButton.interactable = interactable;

            confirmButton.Text = confirm;
            runeNameText.text = L10nManager.Localize($"RUNE_NAME_{item.RuneState.RuneId}");
            currentLevelText.text = $"+{item.RuneState.Level}";

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (runeListSheet.TryGetValue(item.RuneState.RuneId, out var row))
            {
                levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", row.RequiredLevel);
                UpdateGrade(row);
                UpdateAreaIcon((RuneUsePlace)row.UsePlace);
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

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(item.RuneState.RuneId, out var optionRow))
            {
                return;
            }

            if (optionRow.LevelOptionMap.TryGetValue(item.RuneState.Level, out var option))
            {
                if (option.SkillId != 0)
                {
                    var name = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                    var currentValueString = RuneFrontHelper.GetRuneValueString(option);
                    var desc = L10nManager.Localize(
                        $"SKILL_DESCRIPTION_{option.SkillId}", option.SkillChance, option.BuffDuration, currentValueString);
                    var cooldown = $"{L10nManager.Localize($"UI_COOLDOWN")} : {option.SkillCooldown}";
                    skillView.Show(name, desc, cooldown);
                }
                else
                {
                    skillView.Hide();
                }

                foreach (var statView in statViewList)
                {
                    statView.gameObject.SetActive(false);
                }

                for (var i = 0; i < option.Stats.Count; i++)
                {
                    var (statMap, _) = option.Stats[i];
                    statViewList[i].gameObject.SetActive(true);
                    statViewList[i].Show(statMap.StatType, statMap.TotalValueAsLong, true);
                }

                cpText.text = $"<size=80%>CP</size> {option.Cp}";
            }

            _onConfirm = onConfirm;
            _onEnhancement = onEnhancement;
            _onClose = onClose;

            scrollbar.value = 1f;
            base.Show();
            StartCoroutine(CoUpdate(confirmButton.gameObject));
        }

        private void UpdateGrade(RuneListSheet.Row row)
        {
            var data = itemViewDataScriptableObject.GetItemViewData(row.Grade);
            gradeImage.overrideSprite = data.GradeBackground;
            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;

            var color = LocalizationExtensions.GetItemGradeColor(row.Grade);
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{row.Grade}");
            gradeText.color = color;
            runeNameText.color = color;
            subTypeText.color = color;
            spacerImage.color = color;
        }

        private void UpdateAreaIcon(RuneUsePlace runeUsePlace)
        {
            var disableColor = Palette.GetColor(ColorType.TextDenial);
            switch (runeUsePlace)
            {
                case RuneUsePlace.Adventure:
                    adventure.color = Color.white;
                    arena.color = disableColor;
                    raid.color = disableColor;
                    break;
                case RuneUsePlace.Arena:
                    adventure.color = disableColor;
                    arena.color = Color.white;
                    raid.color = disableColor;
                    break;
                case RuneUsePlace.AdventureAndArena:
                    adventure.color = Color.white;
                    arena.color = Color.white;
                    raid.color = disableColor;
                    break;
                case RuneUsePlace.Raid:
                    adventure.color = disableColor;
                    arena.color = disableColor;
                    raid.color = Color.white;
                    break;
                case RuneUsePlace.RaidAndAdventure:
                    adventure.color = Color.white;
                    arena.color = disableColor;
                    raid.color = Color.white;
                    break;
                case RuneUsePlace.RaidAndArena:
                    adventure.color = disableColor;
                    arena.color = Color.white;
                    raid.color = Color.white;
                    break;
                case RuneUsePlace.All:
                    adventure.color = Color.white;
                    arena.color = Color.white;
                    raid.color = Color.white;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runeUsePlace), runeUsePlace, null);
            }
        }

        protected IEnumerator CoUpdate(GameObject target)
        {
            var selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isClickedButtonArea = _isPointerOnScrollArea;
                }

                var current = TouchHandler.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }
                else
                {
                    if (current == target)
                    {
                        yield break;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public void OnEnterButtonArea(bool value)
        {
            _isPointerOnScrollArea = value;
        }
    }
}

using System;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UIEffects;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(RectTransform))]
    public class RuneSlotView : MonoBehaviour
    {
        [SerializeField]
        private RuneType runeType;

        [SerializeField]
        private OptionTagDataScriptableObject optionTagData = null;

        [SerializeField]
        private Image itemImage = null;

        [SerializeField]
        private TextMeshProUGUI enhancementText = null;

        [SerializeField]
        private UIHsvModifier optionTagBg;

        [SerializeField]
        private Image optionTagImage;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewData;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private GameObject lockPriceObject;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private TextMeshProUGUI lockPrice;

        [SerializeField]
        private Image priceIconImage;

        private Action<RuneSlotView> _onClick;
        private Action<RuneSlotView> _onDoubleClick;
        private EventTrigger _eventTrigger;

        public RuneType RuneType => runeType;
        public RectTransform RectTransform { get; private set; }
        public RuneSlot RuneSlot { get; private set; }

        private void Awake()
        {
            _eventTrigger = GetComponent<EventTrigger>();
            if (!_eventTrigger)
            {
                throw new NotFoundComponentException<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(OnClick);
            _eventTrigger.triggers.Add(entry);

            RectTransform = GetComponent<RectTransform>();
        }

        public void Set(
            RuneSlot runeSlot,
            Action<RuneSlotView> onClick,
            Action<RuneSlotView> onDoubleClick)
        {
            RuneSlot = runeSlot;
            _onClick = onClick;
            _onDoubleClick = onDoubleClick;
            UpdateLoading(runeSlot);
            UpdateLockState(runeSlot);
            optionTagBg.gameObject.SetActive(false);
            if (runeSlot.RuneId.HasValue)
            {
                Equip(runeSlot.RuneId.Value);
            }
            else
            {
                Unequip();
            }
        }

        public void Set(
            RuneSlot runeSlot,
            RuneState runeState,
            Action<RuneSlotView> onClick)
        {
            RuneSlot = runeSlot;
            _onClick = onClick;
            _onDoubleClick = null;
            UpdateLoading(runeSlot);
            UpdateLockState(runeSlot);
            optionTagBg.gameObject.SetActive(false);
            if (runeSlot.RuneId.HasValue)
            {
                Equip(runeState);
            }
            else
            {
                Unequip();
            }
        }

        private void UpdateLoading(RuneSlot runeSlot)
        {
            var value = LoadingHelper.UnlockRuneSlot.Any(x => x == runeSlot.Index);
            loadingObject.SetActive(value);
        }

        private void UpdateLockState(RuneSlot runeSlot)
        {
            var isLock = runeSlot.IsLock;
            lockObject.SetActive(isLock);
            if (isLock)
            {
                var cost = 0;
                if (runeSlot.RuneSlotType == RuneSlotType.Ncg)
                {
                    cost = runeSlot.RuneType == RuneType.Stat
                        ? States.Instance.GameConfigState.RuneStatSlotUnlockCost
                        : States.Instance.GameConfigState.RuneSkillSlotUnlockCost;
                    priceIconImage.sprite = SpriteHelper.GetFavIcon("NCG");
                }
                else if (runeSlot.RuneSlotType == RuneSlotType.Crystal)
                {
                    cost = runeSlot.RuneType == RuneType.Stat
                        ? States.Instance.GameConfigState.RuneStatSlotCrystalUnlockCost
                        : States.Instance.GameConfigState.RuneSkillSlotCrystalUnlockCost;
                    priceIconImage.sprite = SpriteHelper.GetFavIcon("CRYSTAL");
                }

                lockPrice.text = $"{cost.ToCurrencyNotation()}";
            }

            if (lockPriceObject != null)
            {
                lockPriceObject.SetActive(isLock);
            }
        }

        private void Equip(int runeId)
        {
            if (!States.Instance.AllRuneState.TryGetRuneState(runeId, out var state))
            {
                return;
            }

            if(!RuneFrontHelper.TryGetRuneIcon(state.RuneId, out var icon))
            {
              return;
            }

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (!runeListSheet.TryGetValue(state.RuneId, out var row))
            {
                return;
            }

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(row.Id, out var optionRow))
            {
                return;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(state.Level, out var option))
            {
                return;
            }

            enhancementText.text = $"+{state.Level}";
            itemImage.enabled = true;
            itemImage.overrideSprite = icon;
            itemImage.SetNativeSize();

            UpdateGrade(row);
            UpdateOptionTag(option, row.Grade);
        }

        private void Equip(RuneState runeState)
        {
            if (runeState is null)
            {
                return;
            }

            if(!RuneFrontHelper.TryGetRuneIcon(runeState.RuneId, out var icon))
            {
                return;
            }

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (!runeListSheet.TryGetValue(runeState.RuneId, out var row))
            {
                return;
            }

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(row.Id, out var optionRow))
            {
                return;
            }

            if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
            {
                return;
            }

            enhancementText.text = $"+{runeState.Level}";
            itemImage.enabled = true;
            itemImage.overrideSprite = icon;
            itemImage.SetNativeSize();

            UpdateGrade(row);
            UpdateOptionTag(option, row.Grade);
        }

        private void Unequip()
        {
            enhancementText.text = string.Empty;
            itemImage.enabled = false;
            itemImage.overrideSprite = RuneFrontHelper.DefaultRuneIcon;
            itemImage.SetNativeSize();
            gradeImage.gameObject.SetActive(false);
        }

        private void UpdateGrade(RuneListSheet.Row row)
        {
            gradeImage.gameObject.SetActive(true);
            var data = itemViewData.GetItemViewData(row.Grade);
            gradeImage.overrideSprite = data.GradeBackground;
            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;
        }

        private void UpdateOptionTag(RuneOptionSheet.Row.RuneOptionInfo option, int grade)
        {
            optionTagBg.gameObject.SetActive(option.SkillId != 0);
            if (option.SkillId != 0)
            {
                var data = optionTagData.GetOptionTagData(grade);
                optionTagImage.sprite = optionTagData.SkillOptionSprite;
                optionTagBg.range = data.GradeHsvRange;
                optionTagBg.hue = data.GradeHsvHue;
                optionTagBg.saturation = data.GradeHsvSaturation;
                optionTagBg.value = data.GradeHsvValue;
            }
        }

        private void OnClick(BaseEventData eventData)
        {
            if (!(eventData is PointerEventData data) ||
                data.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            switch (data.clickCount)
            {
                case 1:
                    _onClick?.Invoke(this);
                    AudioController.PlayClick();
                    break;
                case 2:
                    _onDoubleClick?.Invoke(this);
                    break;
            }
        }
    }
}

using System;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UIEffects;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.UI.Module
{
    using UniRx;

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
        private GameObject wearableImage;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private GameObject lockPriceObject;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private TextMeshProUGUI lockPrice;

        private Action<RuneSlotView> _onClick;
        private Action<RuneSlotView> _onDoubleClick;
        private EventTrigger _eventTrigger;
        private readonly List<IDisposable> _disposables = new();

        public RuneType RuneType => runeType;
        public RectTransform RectTransform { get; private set; }
        public RuneSlot RuneSlot { get; private set; }

        public bool IsWearableImage
        {
            get => wearableImage.activeSelf;
            set => wearableImage.SetActive(value);
        }

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

        private void UpdateLoading()
        {
            if (RuneSlot == null)
            {
                return;
            }

            var value = LoadingHelper.UnlockRuneSlot.Any(x => x == RuneSlot.Index);
            loadingObject.SetActive(value);
        }

        public void Set(
            RuneSlot runeSlot,
            Action<RuneSlotView> onClick,
            Action<RuneSlotView> onDoubleClick)
        {
            RuneSlot = runeSlot;
            _onClick = onClick;
            _onDoubleClick = onDoubleClick;

            UpdateLockState(runeSlot);

            _disposables.DisposeAllAndClear();
            LoadingHelper.UnlockRuneSlot.ObserveAdd().Subscribe(x =>
            {
                UpdateLoading();
            }).AddTo(_disposables);
            UpdateLoading();

            wearableImage.SetActive(false);
            optionTagBg.gameObject.SetActive(false);
            if (runeSlot.IsEquipped(out var state))
            {
                Equip(state);
            }
            else
            {
                Unequip();
            }
        }

        private void UpdateLockState(RuneSlot runeSlot)
        {
            lockObject.SetActive(runeSlot.IsLock);
            if(runeSlot.IsLock && runeSlot.RuneSlotType == RuneSlotType.Ncg)
            {
                lockPriceObject.SetActive(true);
                var cost = runeSlot.RuneType == RuneType.Stat
                    ? States.Instance.GameConfigState.RuneStatSlotUnlockCost
                    : States.Instance.GameConfigState.RuneSkillSlotUnlockCost;
                lockPrice.text = $"{cost}";
            }
            else
            {
                lockPriceObject.SetActive(false);
            }
        }

        private void Equip(RuneState state)
        {
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

using System;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UIEffects;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(RectTransform))]
    public class RuneSlotView : MonoBehaviour
    {
        [SerializeField]
        private OptionTagDataScriptableObject optionTagData = null;

        [SerializeField]
        private Image itemImage = null;

        [SerializeField]
        private TextMeshProUGUI enhancementText = null;

        [SerializeField]
        private Image lockImage = null;

        [SerializeField]
        private UIHsvModifier optionTagBg = null;

        [SerializeField]
        private List<Image> optionTagImages = null;

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
        private TextMeshProUGUI lockPrice;

        private Action<RuneSlotView> _onClick;
        private Action<RuneSlotView> _onDoubleClick;
        private EventTrigger _eventTrigger;
        private RuneType _runeType;

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

        public void Set(
            RuneSlot runeSlot,
            Action<RuneSlotView> onClick,
            Action<RuneSlotView> onDoubleClick)
        {
            RuneSlot = runeSlot;
            _onClick = onClick;
            _onDoubleClick = onDoubleClick;
            lockObject.SetActive(runeSlot.IsLock);
            optionTagBg.gameObject.SetActive(false); // temp
            wearableImage.SetActive(false);
            if (runeSlot.IsEquipped(out var state))
            {
                Equip(state);
            }
            else
            {
                Unequip();
            }
        }

        private void Equip(RuneState state)
        {
            enhancementText.text = $"+{state.Level}";
            if (RuneFrontHelper.TryGetRuneIcon(state.RuneId, out var icon))
            {
                itemImage.overrideSprite = icon;
                itemImage.SetNativeSize();
            }

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            if (runeListSheet.TryGetValue(state.RuneId, out var row))
            {
                UpdateGrade(row);
            }
        }

        private void Unequip()
        {
            enhancementText.text = string.Empty;
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

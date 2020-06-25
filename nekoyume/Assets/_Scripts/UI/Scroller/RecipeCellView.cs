using Assets.SimpleLocalization;
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

namespace Nekoyume.UI.Scroller
{
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

        public readonly Subject<RecipeCellView> OnClick =
            new Subject<RecipeCellView>();

        public bool IsLocked => lockParent.activeSelf;
        public ItemSubType ItemSubType { get; protected set; }
        public ElementalType ElementalType { get; protected set; }
        public StatType StatType { get; protected set; }

        public bool Visible
        {
            get => Mathf.Approximately(canvasGroup.alpha, 1f);
            set => canvasGroup.alpha = value ? 1f : 0f;
        }

        private void Awake()
        {
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (IsLocked)
                    {
                        return;
                    }

                    OnClick.OnNext(this);
                })
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
                    var diff =  unlockStage- stageId;
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
                            unlockStage.ToString());
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

            SetCellViewLocked(value);
        }
    }
}

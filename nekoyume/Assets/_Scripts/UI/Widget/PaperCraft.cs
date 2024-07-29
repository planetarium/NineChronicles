using System;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class PaperCraft : Widget
    {
        [Serializable]
        private class SubTypeButton
        {
            public ItemSubType itemSubType;
            public Button button;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SubTypeButton[] subTypeButtons;

        // [SerializeField]
        // private ConditionalCostButton conditionalCostButton;

        [SerializeField]
        private Button craftButton;

        [SerializeField]
        private Button skillHelpButton;

        [SerializeField]
        private Button skillListButton;

        [SerializeField]
        private TextMeshProUGUI subTypePaperText;

        [SerializeField]
        private TextMeshProUGUI skillText;

        [SerializeField]
        private CustomOutfitScroll outfitScroll;

        [SerializeField]
        private TextMeshProUGUI baseStatText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private TextMeshProUGUI requiredBlockText;

        private CustomOutfit _selectedOutfit;

        private ItemSubType _selectedSubType = ItemSubType.Weapon;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });
            skillHelpButton.onClick.AddListener(() =>
            {
                NcDebug.Log("skillHelpButton onclick");
                // 뭐시기팝업.Show();
            });
            skillListButton.onClick.AddListener(() =>
            {
                Find<SummonSkillsPopup>().Show(TableSheets.Instance.SummonSheet.First);
                NcDebug.Log("skillListButton onclick");
            });
            foreach (var subTypeButton in subTypeButtons)
            {
                subTypeButton.button.onClick.AddListener(() =>
                {
                    OnItemSubtypeSelected(subTypeButton.itemSubType);
                });
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            ReactiveAvatarState.ObservableRelationship
                .Where(_ => isActiveAndEnabled)
                .Subscribe(SetRelationshipView)
                .AddTo(gameObject);

            outfitScroll.OnClick.Subscribe(item =>
            {
                if (_selectedOutfit != null)
                {
                    _selectedOutfit.Selected.Value = false;
                }

                _selectedOutfit = item;
                _selectedOutfit.Selected.Value = true;

                var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet
                    .OrderedList.First(row => row.Relationship >= ReactiveAvatarState.Relationship);
                var equipmentItemId = relationshipRow.GetItemId(_selectedSubType);
                var equipmentItemSheet = TableSheets.Instance.EquipmentItemSheet;
                if (equipmentItemSheet.TryGetValue(equipmentItemId, out var equipmentRow))
                {
                    // TODO: 싹 다 시안에 맞춰서 표현 방식을 변경해야한다. 지금은 외형을 선택하면 시트에서 잘 가져오는지 보려고 했다.
                    baseStatText.SetText($"{equipmentRow.Stat.DecimalStatToString()}");
                    expText.SetText($"EXP: {equipmentRow.Exp}");
                    cpText.SetText($"CP: {relationshipRow.MinCp}~{relationshipRow.MaxCp}");
                    requiredBlockText.SetText(
                        $"{TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r => r.ItemSubType == _selectedSubType).RequiredBlock}");
                }
            }).AddTo(gameObject);
            craftButton.onClick.AddListener(OnClickSubmitButton);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _selectedOutfit = null;
            SetRelationshipView(ReactiveAvatarState.Relationship);
            OnItemSubtypeSelected(ItemSubType.Weapon);
        }

        /// <summary>
        /// 숙련도의 상태를 표시하는 View update 코드이다.
        /// State를 보여주는 기능으로, ActionRenderHandler나 ReactiveAvatarState를 반영해야 한다.
        /// </summary>
        /// <param name="relationship"></param>
        private void SetRelationshipView(long relationship)
        {
            skillText.SetText($"RELATIONSHIP: {relationship}");
        }

        /// <summary>
        /// 어떤 종류의 장비를 만들지 ItemSubType을 선택하면 실행될 콜백, View 업데이트를 한다
        /// </summary>
        /// <param name="type"></param>
        private void OnItemSubtypeSelected(ItemSubType type)
        {
            // TODO: 지금은 그냥 EquipmentItemRecipeSheet에서 해당하는 ItemSubType을 다 뿌리고있다.
            // 나중에 외형을 갖고있는 시트 데이터로 바꿔치기 해야한다.
            _selectedSubType = type;
            subTypePaperText.SetText($"{_selectedSubType} PAPER");
            outfitScroll.UpdateData(TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                .Where(row => row.ItemSubType == _selectedSubType)
                .Select(r => new CustomOutfit(r)));
        }

        private void OnClickSubmitButton()
        {
            if (_selectedOutfit is null)
            {
                OneLineSystem.Push(MailType.System, "select outfit", NotificationCell.NotificationType.Information);
                return;
            }

            if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
            {
                // TODO: 전부 다 CustomEquipmentCraft 관련 sheet에서 가져오게 바꿔야함
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                        TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                            r.ItemSubType == _selectedOutfit.IconRow.Value.ItemSubType).Id,
                        _selectedOutfit.IconRow.Value.Id)
                    .Subscribe();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _selectedOutfit = null;
        }
    }
}

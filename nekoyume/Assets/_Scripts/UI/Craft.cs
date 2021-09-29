using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using System.Text.Json;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using System;
using System.Collections;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using System.Linq;

namespace Nekoyume.UI
{
    using UniRx;
    using Toggle = Module.Toggle;

    public class Craft : Widget
    {
        [SerializeField] private Toggle equipmentToggle = null;
        [SerializeField] private Toggle consumableToggle = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private RecipeScroll recipeScroll = null;
        [SerializeField] private SubRecipeView equipmentSubRecipeView = null;
        [SerializeField] private SubRecipeView consumableSubRecipeView = null;

        [SerializeField] private CanvasGroup canvasGroup = null;

        private bool _isEquipment;

        public static RecipeModel SharedModel = null;

        private const string ConsumableRecipeGroupPath = "Recipe/ConsumableRecipeGroup";

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });

            CloseWidget = () =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            };

            equipmentToggle.onValueChanged.AddListener(value =>
            {
                if (!value) return;

                if (!_isEquipment)
                {
                    _isEquipment = true;

                    if (Animator.GetBool("FirstClicked"))
                    {
                        AudioController.PlayClick();
                        Animator.SetTrigger("EquipmentClick");
                    }
                }
            });

            consumableToggle.onValueChanged.AddListener(value =>
            {
                if (!value) return;

                if (_isEquipment)
                {
                    _isEquipment = false;
                    AudioController.PlayClick();
                    if (Animator.GetBool("FirstClicked"))
                    {
                        Animator.SetTrigger("ConsumableClick");
                    }
                    else
                    {
                        Animator.SetBool("FirstClicked", true);
                    }
                }
            });

            equipmentSubRecipeView.CombinationActionSubject
                .Subscribe(CombinationEquipmentAction)
                .AddTo(gameObject);

            consumableSubRecipeView.CombinationActionSubject
                .Subscribe(CombinationConsumableAction)
                .AddTo(gameObject);
        }

        protected override void OnDisable()
        {
            Animator.SetBool("FirstClicked", false);
            Animator.ResetTrigger("EquipmentClick");
            Animator.ResetTrigger("ConsumableClick");
            base.OnDisable();
        }

        public override void Initialize()
        {
            LoadRecipeModel();
            SharedModel.SelectedRow
                .Subscribe(SetSubRecipe)
                .AddTo(gameObject);
            ReactiveAvatarState.Address.Subscribe(address =>
            {
                if (address.Equals(default)) return;
                SharedModel.LoadRecipeVFXSkipList();
            }).AddTo(gameObject);

            recipeScroll.InitializeNotification();
            ReactiveAvatarState.QuestList
                .Subscribe(SubscribeQuestList)
                .AddTo(gameObject);
        }

        public void Show(int equipmentRecipeId)
        {
            Show();

            if (!Game.Game.instance.TableSheets
                .EquipmentItemRecipeSheet.TryGetValue(equipmentRecipeId, out var row))
            {
                return;
            }

            var itemRow = row.GetResultEquipmentItemRow();
            recipeScroll.ShowAsEquipment(itemRow.ItemSubType, true);
            var group = RecipeModel.GetEquipmentGroup(row.ResultEquipmentId);
            recipeScroll.GoToRecipeGroup(group);
            if (SharedModel.RecipeVFXSkipList.Contains(equipmentRecipeId))
            {
                SharedModel.SelectedRow.Value = row;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<CombinationLoadingScreen>().OnDisappear = OnNPCDisappear;
            equipmentSubRecipeView.gameObject.SetActive(false);
            consumableSubRecipeView.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);

            // Toggles can be switched after enabled.
            ShowEquipment();
            if (equipmentToggle.isOn)
            {
                _isEquipment = true;
            }
            else
            {
                equipmentToggle.isOn = true;
            }

            if (!Game.Game.instance.Stage.TutorialController.IsPlaying)
            {
                HelpPopup.HelpMe(100016, true);
            }

            var audioController = AudioController.instance;
            var musicName = AudioController.MusicCode.Combination;
            if (!audioController.CurrentPlayingMusicName.Equals(musicName))
            {
                AudioController.instance.PlayMusic(musicName);
            }
        }

        private void ShowEquipment()
        {
            equipmentSubRecipeView.ResetSelectedIndex();
            recipeScroll.ShowAsEquipment(ItemSubType.Weapon, true);
            SharedModel.SelectedRow.Value = null;
        }

        private void ShowConsumable()
        {
            consumableSubRecipeView.ResetSelectedIndex();
            recipeScroll.ShowAsFood(StatType.HP, true);
            SharedModel.SelectedRow.Value = null;
        }

        private void SetSubRecipe(SheetRow<int> row)
        {
            if (row is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                equipmentSubRecipeView.gameObject.SetActive(true);
                consumableSubRecipeView.gameObject.SetActive(false);
                equipmentSubRecipeView.SetData(equipmentRow, equipmentRow.SubRecipeIds);
            }
            else if (row is ConsumableItemRecipeSheet.Row consumableRow)
            {
                equipmentSubRecipeView.gameObject.SetActive(false);
                consumableSubRecipeView.gameObject.SetActive(true);
                consumableSubRecipeView.SetData(consumableRow, null);
            }
            else
            {
                equipmentSubRecipeView.gameObject.SetActive(false);
                consumableSubRecipeView.gameObject.SetActive(false);
            }
        }

        private void LoadRecipeModel()
        {
            var jsonAsset = Resources.Load<TextAsset>(ConsumableRecipeGroupPath);
            var group = jsonAsset is null ?
                default : JsonSerializer.Deserialize<CombinationRecipeGroup>(jsonAsset.text);

            SharedModel = new RecipeModel(
                Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values,
                group.Groups);
        }

        private void SubscribeQuestList(QuestList questList)
        {
            var quest = questList?
                .OfType<CombinationEquipmentQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.StageId)
                .FirstOrDefault();

            if (quest is null ||
                !Game.Game.instance.TableSheets.EquipmentItemRecipeSheet
                .TryGetValue(quest.RecipeId, out var row) ||
                !States.Instance.CurrentAvatarState.worldInformation
                .TryGetLastClearedStageId(out var clearedStage))
            {
                SharedModel.NotifiedRow.Value = null;
                return;
            }

            var stageId = row.UnlockStage;
            SharedModel.NotifiedRow.Value = clearedStage >= stageId ? row : null;
        }

        private void CombinationEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!equipmentSubRecipeView.CheckSubmittable(out var errorMessage, out var slotIndex))
            {
                OneLinePopup.Push(MailType.System, errorMessage);
                return;
            }

            OnCombinationAction(recipeInfo);

            var tableSheets = Game.Game.instance.TableSheets;

            var equipmentRow = tableSheets.EquipmentItemRecipeSheet[recipeInfo.RecipeId];
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow.GetResultEquipmentItemRow(), Guid.Empty, default);
            var requiredBlockIndex = equipmentRow.RequiredBlockIndex;
            if (recipeInfo.SubRecipeId.HasValue)
            {
                var subRecipeRow = tableSheets.EquipmentItemSubRecipeSheetV2[recipeInfo.SubRecipeId.Value];
                requiredBlockIndex += subRecipeRow.RequiredBlockIndex;
            }

            var slots = Find<CombinationSlots>();
            slots.SetCaching(slotIndex, true, requiredBlockIndex, itemUsable:equipment);

            equipmentSubRecipeView.UpdateView();
            Game.Game.instance.ActionManager.CombinationEquipment(
                recipeInfo.RecipeId,
                slotIndex,
                recipeInfo.SubRecipeId);

            StartCoroutine(CoCombineNPCAnimation(equipment, requiredBlockIndex));
        }

        private void CombinationConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!consumableSubRecipeView.CheckSubmittable(out var errorMessage, out var slotIndex))
            {
                OneLinePopup.Push(MailType.System, errorMessage);
                return;
            }

            OnCombinationAction(recipeInfo);
            var consumableRow = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet[recipeInfo.RecipeId];
            var consumable = (Consumable)ItemFactory.CreateItemUsable(
                consumableRow.GetResultConsumableItemRow(), Guid.Empty, default);
            var requiredBlockIndex = consumableRow.RequiredBlockIndex;
            var slots = Find<CombinationSlots>();
            slots.SetCaching(slotIndex, true, requiredBlockIndex, itemUsable:consumable);

            consumableSubRecipeView.UpdateView();
            Game.Game.instance.ActionManager.CombinationConsumable(
                recipeInfo.RecipeId,
                slotIndex);

            StartCoroutine(CoCombineNPCAnimation(consumable, requiredBlockIndex, true));
        }

        private void OnCombinationAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(agentAddress, -recipeInfo.CostAP);

            foreach (var (material, count) in recipeInfo.Materials)
            {
                LocalLayerModifier.RemoveItem(avatarAddress, material, count);
            }
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, long blockIndex, bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(itemBase.ItemType, quote);
        }

        private void OnNPCDisappear()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Pop();
        }

        public void TutorialActionClickFirstRecipeCellView()
        {
            SharedModel.SelectedRow.Value = SharedModel.RecipeForTutorial;
            SharedModel.SelectedRecipeCell.Unlock();
        }

        public void TutorialActionClickCombinationSubmitButton()
        {
            equipmentSubRecipeView.CombineCurrentRecipe();
        }

        public void TutorialActionCloseCombination()
        {
            Close(true);
            Game.Event.OnRoomEnter.Invoke(true);
        }
    }
}

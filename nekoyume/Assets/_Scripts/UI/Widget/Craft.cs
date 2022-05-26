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
    using Libplanet;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Security.Cryptography;
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
                .Subscribe(OnClickEquipmentAction)
                .AddTo(gameObject);

            consumableSubRecipeView.CombinationActionSubject
                .Subscribe(OnClickConsumableAction)
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
            SharedModel.UnlockedRecipes
                .Subscribe(list =>
                {
                    if (list != null &&
                        SharedModel.SelectedRow.Value != null &&
                        list.Contains(SharedModel.SelectedRow.Value.Key))
                    {
                        SetSubRecipe(SharedModel.SelectedRow.Value);
                    }
                })
                .AddTo(gameObject);

            ReactiveAvatarState.Address.Subscribe(address =>
            {
                if (address.Equals(default)) return;
                SharedModel.UpdateUnlockedRecipesAsync(address);
            }).AddTo(gameObject);

            ReactiveAvatarState.QuestList
                .Subscribe(SubscribeQuestList)
                .AddTo(gameObject);

            ReactiveAvatarState.Inventory
                .Subscribe(_ =>
                {
                    if (equipmentSubRecipeView.gameObject.activeSelf)
                    {
                        equipmentSubRecipeView.UpdateView();
                    }
                    else if (consumableSubRecipeView.gameObject.activeSelf)
                    {
                        consumableSubRecipeView.UpdateView();
                    }
                })
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
            if (SharedModel.UnlockedRecipes.Value.Contains(equipmentRecipeId))
            {
                SharedModel.SelectedRow.Value = row;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
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
                HelpTooltip.HelpMe(100016, true);
            }

            var audioController = AudioController.instance;
            var musicName = AudioController.MusicCode.Combination;
            if (!audioController.CurrentPlayingMusicName.Equals(musicName))
            {
                AudioController.instance.PlayMusic(musicName);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            SharedModel.SelectedRow.Value = null;
            base.Close(ignoreCloseAnimation);
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

        private void OnClickEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var requirementSheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
            var recipeSheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
            if (!recipeSheet.TryGetValue(recipeInfo.RecipeId, out var recipeRow))
            {
                return;
            }

            var resultItemRow = recipeRow.GetResultEquipmentItemRow();
            if (!requirementSheet.TryGetValue(resultItemRow.Id, out var requirementRow))
            {
                CombinationEquipmentAction(recipeInfo);
                return;
            }

            CombinationEquipmentAction(recipeInfo);
        }

        private void OnClickConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var requirementSheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
            var recipeSheet = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet;
            if (!recipeSheet.TryGetValue(recipeInfo.RecipeId, out var recipeRow))
            {
                return;
            }

            var resultItemRow = recipeRow.GetResultConsumableItemRow();
            if (!requirementSheet.TryGetValue(resultItemRow.Id, out var requirementRow))
            {
                CombinationConsumableAction(recipeInfo);
                return;
            }

            CombinationConsumableAction(recipeInfo);
        }

        private void CombinationEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!equipmentSubRecipeView.CheckSubmittable(out var errorMessage, out var slotIndex))
            {
                OneLineSystem.Push(MailType.System, errorMessage, NotificationCell.NotificationType.Alert);
                return;
            }

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

            equipmentSubRecipeView.UpdateView();
            var insufficientMaterials = recipeInfo.ReplacedMaterials;
            if (insufficientMaterials.Any())
            {
                Find<ReplaceMaterialPopup>().Show(insufficientMaterials,
                    () =>
                    {
                        var slots = Find<CombinationSlotsPopup>();
                        slots.SetCaching(slotIndex, true, requiredBlockIndex, itemUsable: equipment);
                        Game.Game.instance.ActionManager
                            .CombinationEquipment(recipeInfo, slotIndex, true).Subscribe();
                        StartCoroutine(CoCombineNPCAnimation(equipment, requiredBlockIndex));
                    });
            }
            else
            {
                var slots = Find<CombinationSlotsPopup>();
                slots.SetCaching(slotIndex, true, requiredBlockIndex, itemUsable: equipment);
                Game.Game.instance.ActionManager.CombinationEquipment(recipeInfo, slotIndex, false)
                    .Subscribe();
                StartCoroutine(CoCombineNPCAnimation(equipment, requiredBlockIndex));
            }
        }

        private void CombinationConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            if (!consumableSubRecipeView.CheckSubmittable(out var errorMessage, out var slotIndex))
            {
                OneLineSystem.Push(MailType.System, errorMessage, NotificationCell.NotificationType.Alert);
                return;
            }

            var consumableRow = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet[recipeInfo.RecipeId];
            var consumable = (Consumable)ItemFactory.CreateItemUsable(
                consumableRow.GetResultConsumableItemRow(), Guid.Empty, default);
            var requiredBlockIndex = consumableRow.RequiredBlockIndex;
            var slots = Find<CombinationSlotsPopup>();
            slots.SetCaching(slotIndex, true, requiredBlockIndex, itemUsable:consumable);

            consumableSubRecipeView.UpdateView();
            Game.Game.instance.ActionManager.CombinationConsumable(recipeInfo, slotIndex).Subscribe();

            StartCoroutine(CoCombineNPCAnimation(consumable, requiredBlockIndex, true));
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, long blockIndex, bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(null);
            loadingScreen.OnDisappear = OnNPCDisappear;
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
            SharedModel.SelectedRecipeCell.UnlockDummyLocked();
        }

        public void TutorialActionClickCombinationSubmitButton()
        {
            equipmentSubRecipeView.CombineCurrentRecipe();
        }

        public void TutorialActionCloseCombination()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Close(true);
            Game.Event.OnRoomEnter.Invoke(true);
        }
    }
}

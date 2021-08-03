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
            });
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
        }

        private void ShowEquipment()
        {
            recipeScroll.ShowAsEquipment(ItemSubType.Weapon, true);
            SharedModel.SelectedRow.Value = null;
        }

        private void ShowConsumable()
        {
            recipeScroll.ShowAsFood(StatType.HP, true);
            SharedModel.SelectedRow.Value = null;
        }

        private void SetSubRecipe(SheetRow<int> row)
        {
            if (row is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                equipmentSubRecipeView.SetData(equipmentRow, equipmentRow.SubRecipeIds);
                equipmentSubRecipeView.gameObject.SetActive(true);
                consumableSubRecipeView.gameObject.SetActive(false);
            }
            else if (row is ConsumableItemRecipeSheet.Row consumableRow)
            {
                consumableSubRecipeView.SetData(consumableRow, null);
                equipmentSubRecipeView.gameObject.SetActive(false);
                consumableSubRecipeView.gameObject.SetActive(true);
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

        private void CombinationEquipmentAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var slotIndex = OnCombinationAction(recipeInfo);
            if (slotIndex < 0)
            {
                return;
            }
            equipmentSubRecipeView.UpdateView();

            Game.Game.instance.ActionManager.CombinationEquipment(
                recipeInfo.RecipeId,
                slotIndex,
                recipeInfo.SubRecipeId);

            var equipmentRow = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet[recipeInfo.RecipeId];
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow.GetResultItemEquipmentRow(), Guid.Empty, default);

            StartCoroutine(CoCombineNPCAnimation(equipment));
        }

        private void CombinationConsumableAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var slotIndex = OnCombinationAction(recipeInfo);
            if (slotIndex < 0)
            {
                return;
            }
            consumableSubRecipeView.UpdateView();

            Game.Game.instance.ActionManager.CombinationConsumable(
                recipeInfo.RecipeId,
                slotIndex);

            var consumableRow = Game.Game.instance.TableSheets.ConsumableItemRecipeSheet[recipeInfo.RecipeId];
            var consumable = (Consumable) ItemFactory.CreateItemUsable(
                consumableRow.GetResultItemConsumableRow(), Guid.Empty, default);

            StartCoroutine(CoCombineNPCAnimation(consumable, true));
        }

        private int OnCombinationAction(SubRecipeView.RecipeInfo recipeInfo)
        {
            var slots = Find<CombinationSlots>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return -1;
            }

            slots.SetCaching(slotIndex, true);

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalLayerModifier.ModifyAgentGold(agentAddress, -recipeInfo.CostNCG);
            LocalLayerModifier.ModifyAvatarActionPoint(agentAddress, -recipeInfo.CostAP);

            foreach (var (material, count) in recipeInfo.Materials)
            {
                LocalLayerModifier.RemoveItem(avatarAddress, material, count);
            }

            return slotIndex;
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.AnimateNPC();
        }

        private void OnNPCDisappear()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Pop();
        }
    }
}

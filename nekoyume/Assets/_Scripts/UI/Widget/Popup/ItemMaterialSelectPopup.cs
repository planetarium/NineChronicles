using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData.Event;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class ItemMaterialSelectPopup : PopupWidget
    {
        [SerializeField] private SimpleCountableItemView itemView;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private ConditionalButton combineButton;
        [SerializeField] private TextMeshProUGUI selectCountText;
        [SerializeField] private TextMeshProUGUI requiredCountText;
        [SerializeField] private ItemMaterialSelectScroll scroll;
        [SerializeField] private Button closeButton;

        private List<ItemMaterialSelectScroll.Model> Models { get; set; }

        private readonly List<IDisposable> _disposablesForShow = new();
        private readonly Dictionary<int, Dictionary<int, int>> _selectedMaterialsByRecipe = new();

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }

        public void Show(
            EventMaterialItemRecipeSheet.Row recipeRow,
            Action<Dictionary<int, int>> onSubmit)
        {
            combineButton.Interactable = false;
            _disposablesForShow.DisposeAllAndClear();

            var material = ItemFactory.CreateMaterial(
                Game.Game.instance.TableSheets.MaterialItemSheet,
                recipeRow.ResultMaterialItemId
            );
            var resultItemCount = recipeRow.ResultMaterialItemCount;
            var resultCountableItem = new CountableItem(material, resultItemCount);
            itemView.SetData(resultCountableItem);
            itemNameText.text = Game.Game.instance.TableSheets
                .ItemSheet[recipeRow.ResultMaterialItemId].GetLocalizedName(false, false);

            var requiredItemCount = recipeRow.RequiredMaterialsCount;
            selectCountText.text = "0";
            requiredCountText.text = $"/{requiredItemCount}";

            var inventoryItems = States.Instance.CurrentAvatarState.inventory.Items;
            var lastSelectedMaterials = _selectedMaterialsByRecipe.GetValueOrDefault(recipeRow.Id);
            var models = recipeRow.RequiredMaterialsId
                .Select(id =>
                {
                    var itemBase = ItemFactory.CreateMaterial(Game.Game.instance.TableSheets.MaterialItemSheet[id]);
                    var itemCount = inventoryItems.FirstOrDefault(item => Equals(item.item.Id, itemBase.Id))?.count ?? 0;
                    var countableItem = new CountableItem(itemBase, itemCount);
                    var lastSelectedCount = lastSelectedMaterials?.GetValueOrDefault(id) ?? 0;
                    return new ItemMaterialSelectScroll.Model(countableItem, Mathf.Min(lastSelectedCount, itemCount));
                })
                .OrderByDescending(model => model.Item.Count.Value)
                .ThenBy(model => model.Item.ItemBase.Value.Id)
                .ToList();
            Models = models;
            Observable.NextFrame().Subscribe(_ =>
            {
                scroll.UpdateData(Models, true);
                scroll.OnChangeCount.Subscribe(value =>
                {
                    var totalSelectedCount = Models.Sum(model => model.SelectedCount.Value) - value.Item1.SelectedCount.Value;
                    var itemCount = value.Item1.Item.Count.Value;
                    var maxSelectableCount = Mathf.Min(requiredItemCount - totalSelectedCount, itemCount);

                    value.Item1.SelectedCount.Value = Mathf.Clamp(value.Item2, 0, maxSelectableCount);
                    CheckSelectedCount(requiredItemCount);
                }).AddTo(_disposablesForShow);

                CheckSelectedCount(requiredItemCount);
            });

            combineButton.OnSubmitSubject.Subscribe(_ =>
                {
                    var materials = Models
                        .Where(model => model.SelectedCount.Value > 0)
                        .ToDictionary(
                            model => model.Item.ItemBase.Value.Id,
                            model => model.SelectedCount.Value);
                    _selectedMaterialsByRecipe[recipeRow.Id] = materials;
                    onSubmit(materials);
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForShow);

            base.Show();
        }

        private void CheckSelectedCount(int requiredItemCount)
        {
            var totalSelectedCount = Models.Sum(model => model.SelectedCount.Value);
            selectCountText.text = totalSelectedCount.ToString();
            combineButton.Interactable = totalSelectedCount == requiredItemCount;
        }
    }
}

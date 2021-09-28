using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Nekoyume.UI.Scroller
{
    using Nekoyume.UI.Module;
    using UniRx;

    public class RecipeScroll : RectScroll<RecipeRow.Model, RecipeScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [Serializable]
        private struct EquipmentCategoryToggle
        {
            public Toggle Toggle;
            public ItemSubType Type;
            public Image IndicatorImage;
        }

        [Serializable]
        private struct ConsumableCategoryToggle
        {
            public Toggle Toggle;
            public StatType Type;
        }

        [SerializeField]
        private List<EquipmentCategoryToggle> equipmentCategoryToggles = null;

        [SerializeField]
        private List<ConsumableCategoryToggle> consumableCategoryToggles = null;

        [SerializeField]
        private GameObject equipmentTab = null;

        [SerializeField]
        private GameObject consumableTab = null;

        [SerializeField]
        private GameObject emptyObject = null;

        [SerializeField]
        private float animationInterval = 0.3f;

        private Coroutine _animationCoroutine = null;

        protected void Awake()
        {
            foreach (var categoryToggle in equipmentCategoryToggles)
            {
                var type = categoryToggle.Type;
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ShowAsEquipment(type);
                });
            }

            foreach (var categoryToggle in consumableCategoryToggles)
            {
                var type = categoryToggle.Type;
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ShowAsFood(type);
                });
            }
        }

        public void InitializeNotification()
        {
            Craft.SharedModel.NotifiedRow
                .Subscribe(SubscribeNotifiedRow)
                .AddTo(gameObject);
        }

        public void ShowAsEquipment(ItemSubType type, bool updateToggle = false)
        {
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(true);
            consumableTab.SetActive(false);
            if (updateToggle)
            {
                var toggle = equipmentCategoryToggles.Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsEquipment(type);
                    return;
                }
                toggle.Toggle.isOn = true;
                return;
            }

            var items = Craft.SharedModel.EquipmentRecipeMap.Values
                .Where(x => x.ItemSubType == type)
                ?? Enumerable.Empty<RecipeRow.Model>();

            emptyObject.SetActive(!items.Any());
            Show(items, true);
            AnimateScroller();
        }

        public void ShowAsFood(StatType type, bool updateToggle = false)
        {
            Craft.SharedModel.SelectedRow.Value = null;
            equipmentTab.SetActive(false);
            consumableTab.SetActive(true);
            if (updateToggle)
            {
                var toggle = consumableCategoryToggles.Find(x => x.Type == type);
                if (toggle.Toggle.isOn)
                {
                    ShowAsFood(type);
                    return;
                }
                toggle.Toggle.isOn = true;
                return;
            }

            var items = Craft.SharedModel.ConsumableRecipeMap.Values
                .Where(x => x.StatType == type)
                ?? Enumerable.Empty<RecipeRow.Model>();

            emptyObject.SetActive(!items.Any());
            Show(items, true);
            AnimateScroller();
        }

        private void AnimateScroller()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(CoAnimateScroller());
        }

        private IEnumerator CoAnimateScroller()
        {
            var rows = GetComponentsInChildren<RecipeRow>();
            var wait = new WaitForSeconds(animationInterval);

            Scroller.Draggable = false;
            foreach (var row in rows)
            {
                row.HideWithAlpha();
            }

            yield return null;
            Relayout();

            foreach (var row in rows)
            {
                row.ShowAnimation();
                yield return wait;
            }
            Scroller.Draggable = true;

            _animationCoroutine = null;
        }

        public void SubscribeNotifiedRow(SheetRow<int> row)
        {
            if (!(row is EquipmentItemRecipeSheet.Row equipmentRow))
            {
                foreach (var toggle in equipmentCategoryToggles)
                {
                    toggle.IndicatorImage.enabled = false;
                }

                return;
            }

            var resultItem = equipmentRow.GetResultEquipmentItemRow();
            foreach (var toggle in equipmentCategoryToggles)
            {
                toggle.IndicatorImage.enabled =
                    toggle.Type == resultItem.ItemSubType;
            }
        }

        public void GoToRecipeGroup(string equipmentRecipeGroup)
        {
            if (!Craft.SharedModel.EquipmentRecipeMap
                .TryGetValue(equipmentRecipeGroup, out var model))
            {
                return;
            }

            JumpTo(model);
        }
    }
}

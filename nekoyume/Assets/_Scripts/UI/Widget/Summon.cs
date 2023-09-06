using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.UI
{
    using UniRx;
    public class Summon : Widget
    {
        [Serializable]
        private class DrawItems
        {
            public Button infoButton;
            public TextMeshProUGUI nameText;
            public SimpleCostButton draw1Button;
            public SimpleCostButton draw10Button;
        }

        [SerializeField] private CostIconDataScriptableObject costIconData;
        [SerializeField] private Button closeButton;
        [SerializeField] private DrawItems[] drawItems;
        public int normalSummonId;
        public int goldenSummonId;

        public const int SummonGroup = 2;
        private SummonSheet.Row[] _summonRows;
        private bool _isInitialized;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            });
            CloseWidget = () =>
            {
                Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            };

            LoadingHelper.Summon.Subscribe(value =>
            {
                foreach (var item in drawItems)
                {
                    item.draw1Button.Loading = value;
                    item.draw10Button.Loading = value;
                }
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            closeButton.interactable = true;

            if (!_isInitialized)
            {
                _isInitialized = true;

                var summonSheet = Game.Game.instance.TableSheets.SummonSheet;
                _summonRows = new[] { summonSheet[normalSummonId], summonSheet[goldenSummonId] };

                Subscribe();
            }

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            SetMaterialAssets(States.Instance.CurrentAvatarState.inventory);
        }

        private void Subscribe()
        {
            _disposables.DisposeAllAndClear();
            for (int i = 0; i < SummonGroup; i++)
            {
                var items = drawItems[i];
                var summonRow = _summonRows[i];

                var costType = (CostType)summonRow.CostMaterial;
                var cost = summonRow.CostMaterialCount;

                items.infoButton.OnClickAsObservable()
                    .Subscribe(_ => Find<SummonDetailPopup>().Show(summonRow))
                    .AddTo(_disposables);
                items.nameText.text = summonRow.GetLocalizedName();

                items.draw1Button.SetCost(costType, cost);
                items.draw10Button.SetCost(costType, cost * 10);

                items.draw1Button.OnSubmitSubject
                    .Subscribe(_ => AuraSummonAction(summonRow.GroupId, 1))
                    .AddTo(_disposables);
                items.draw10Button.OnSubmitSubject
                    .Subscribe(_ => AuraSummonAction(summonRow.GroupId, 10))
                    .AddTo(_disposables);
            }
        }

        public void AuraSummonAction(int groupId, int drawCount)
        {
            // Check material enough
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var tableSheets = Game.Game.instance.TableSheets;
            var summonRow = tableSheets.SummonSheet[groupId];
            var materialRow = tableSheets.MaterialItemSheet[summonRow.CostMaterial];

            var costCount = summonRow.CostMaterialCount * drawCount;
            var count = inventory.TryGetFungibleItems(materialRow.ItemId, out var items)
                ? items.Sum(x => x.count)
                : 0;

            if (count < costCount)
            {
                // Not enough
                Debug.LogError($"Group : {groupId}, Material : {materialRow.GetLocalizedName()}, has :{count}.");
                return;
            }

            ActionManager.Instance.AuraSummon(groupId, drawCount).Subscribe();
            LoadingHelper.Summon.Value = true;
            SetMaterialAssets(States.Instance.CurrentAvatarState.inventory);
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = false;
            SetMaterialAssets(States.Instance.CurrentAvatarState.inventory);

            var summonRow = Game.Game.instance.TableSheets.SummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(summonRow, summonCount, random);
            Find<SummonResultPopup>().Show(summonRow, resultList);
        }

        private static List<Equipment> SimulateEquipment(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;
            var dummyAgentState = new AgentState(new Address());

            var resultList = new List<Equipment>();
            for (int i = 0; i < summonCount; i++)
            {
                var recipeId = SummonHelper.PickAuraSummonRecipe(summonRow, random);

                var recipeRow = tableSheets.EquipmentItemRecipeSheet[recipeId];
                var subRecipeRow = tableSheets.EquipmentItemSubRecipeSheetV2[recipeRow.SubRecipeIds[0]];
                var equipmentRow = tableSheets.EquipmentItemSheet[recipeRow.ResultEquipmentId];

                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentRow,
                    random.GenerateRandomGuid(),
                    0
                );

                AuraSummon.AddAndUnlockOption(
                    dummyAgentState,
                    equipment,
                    random,
                    subRecipeRow,
                    optionSheet,
                    skillSheet);

                resultList.Add(equipment);
            }

            return resultList.OrderByDescending(row => row.Grade).ToList();
        }

        private void SetMaterialAssets(Inventory inventory)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            var materials = _summonRows.Select(row =>
            {
                var icon = costIconData.GetIcon((CostType)row.CostMaterial);
                var count = inventory.GetMaterialCount(row.CostMaterial);
                return (icon, count);
            }).ToArray();

            var headerMenu = Find<HeaderMenuStatic>();
            for (var i = 0; i < SummonGroup; i++)
            {
                var (icon, count) = materials[i];
                headerMenu.MaterialAssets[i].SetMaterial(icon, count);
            }
        }
    }
}

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

        [SerializeField] private Button closeButton;
        [SerializeField] private DrawItems[] drawItems;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

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

            var inventory = States.Instance.CurrentAvatarState.inventory;
            var auraSummonSheet = Game.Game.instance.TableSheets.AuraSummonSheet;

            var groupIds = new[] { 10001, 10002 };
            var summonRows = groupIds.Select(id => auraSummonSheet[id]).ToArray();

            _disposables.DisposeAllAndClear();
            var min = Mathf.Min(summonRows.Length, drawItems.Length);
            for (int i = 0; i < min; i++)
            {
                Subscribe(drawItems[i], summonRows[i]);

                var count = inventory.GetMaterialCount(summonRows[i].CostMaterial);
                Debug.LogError($"Group : {summonRows[i].GroupId}, Material : {summonRows[i].CostMaterial}, has :{count}.");
            }
        }

        private void Subscribe(DrawItems items, AuraSummonSheet.Row summonRow)
        {
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

        public void AuraSummonAction(int groupId, int drawCount)
        {
            // Check material enough
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var tableSheets = Game.Game.instance.TableSheets;
            var summonRow = tableSheets.AuraSummonSheet[groupId];
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
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = false;

            var groupId = eval.Action.GroupId;
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(groupId, summonCount, random);
            Find<SummonResultPopup>().Show(groupId, resultList);
        }

        private static List<Equipment> SimulateEquipment(
            int groupId,
            int summonCount,
            IRandom random)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var skillSheet = tableSheets.SkillSheet;
            var dummyAgentState = new AgentState(new Address());

            var summonRow = tableSheets.AuraSummonSheet[groupId];

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
    }
}

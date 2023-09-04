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
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Summon : Widget
    {
        [Serializable]
        private class DrawItems
        {
            public DrawItemModel model;
            public Button draw1Button;
            public Button draw10Button;
        }

        [Serializable]
        private class DrawItemModel
        {
            public int groupId;
            // 재료
            // 또는 row
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private DrawItems leftDrawItems;
        [SerializeField] private DrawItems rightDrawItems;

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

            leftDrawItems.draw1Button.onClick.AddListener(() =>
            {
                AuraSummonAction(leftDrawItems.model.groupId, 1);
            });
            leftDrawItems.draw10Button.onClick.AddListener(() =>
            {
                AuraSummonAction(leftDrawItems.model.groupId, 10);
            });

            rightDrawItems.draw1Button.onClick.AddListener(() =>
            {
                AuraSummonAction(rightDrawItems.model.groupId, 1);
            });
            rightDrawItems.draw10Button.onClick.AddListener(() =>
            {
                AuraSummonAction(rightDrawItems.model.groupId, 10);
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            closeButton.interactable = true;

            var groupIds = new[] { 10001, 10002 };
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var tableSheets = Game.Game.instance.TableSheets;

            foreach (var groupId in groupIds)
            {
                // check material enough
                var summonRow = tableSheets.AuraSummonSheet[groupId];
                var materialRow = tableSheets.MaterialItemSheet[summonRow.CostMaterial];
                var count = inventory.TryGetFungibleItems(materialRow.ItemId, out var items)
                        ? items.Sum(x => x.count)
                        : 0;
                Debug.LogError($"Group : {groupId}, Material : {materialRow.GetLocalizedName()}, has :{count}.");
            }
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
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
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

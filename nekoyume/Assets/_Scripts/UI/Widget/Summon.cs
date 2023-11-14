using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
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

        [SerializeField] private Button closeButton;
        [SerializeField] private DrawItems[] drawItems;
        public int normalSummonId;
        public int goldenSummonId;

        public const int SummonGroup = 2;
        private static readonly int[] Counts = { 1, 10 };
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

            var buttons = new[]
            {
                drawItems[0].draw1Button, drawItems[0].draw10Button,
                drawItems[1].draw1Button, drawItems[1].draw10Button
            };
            ButtonSubscribe(buttons, gameObject);
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

                _disposables.DisposeAllAndClear();
                for (int i = 0; i < SummonGroup; i++)
                {
                    var items = drawItems[i];
                    var summonRow = _summonRows[i];

                    items.infoButton.OnClickAsObservable()
                        .Subscribe(_ => Find<SummonDetailPopup>().Show(summonRow))
                        .AddTo(_disposables);
                    items.nameText.text = summonRow.GetLocalizedName();

                    ButtonSubscribe(items.draw1Button, summonRow, 1, _disposables);
                    ButtonSubscribe(items.draw10Button, summonRow, 10, _disposables);
                }
            }
            else
            {
                foreach (var drawItem in drawItems)
                {
                    drawItem.draw1Button.UpdateObjects();
                    drawItem.draw10Button.UpdateObjects();
                }
            }

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            SetMaterialAssets();
        }

        private void AuraSummonAction(int groupId, int summonCount)
        {
            // Check material enough
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var tableSheets = Game.Game.instance.TableSheets;
            var summonRow = tableSheets.SummonSheet[groupId];
            var materialRow = tableSheets.MaterialItemSheet[summonRow.CostMaterial];

            var totalCost = summonRow.CostMaterialCount * summonCount;
            var count = inventory.TryGetFungibleItems(materialRow.ItemId, out var items)
                ? items.Sum(x => x.count)
                : 0;

            if (count < totalCost)
            {
                // Not enough
                Debug.LogError($"Group : {groupId}, Material : {materialRow.GetLocalizedName()}, has :{count}.");
                return;
            }

            ActionManager.Instance.AuraSummon(groupId, summonCount).Subscribe();
            LoadingHelper.Summon.Value = new Tuple<int, int>(summonRow.CostMaterial, totalCost);
            SetMaterialAssets();
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = null;
            SetMaterialAssets();
            foreach (var drawItem in drawItems)
            {
                drawItem.draw1Button.UpdateObjects();
                drawItem.draw10Button.UpdateObjects();
            }

            var summonRow = Game.Game.instance.TableSheets.SummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(summonRow, summonCount, random, eval.BlockIndex);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        private static List<Equipment> SimulateEquipment(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            long blockIndex)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var addressHex = $"[{States.Instance.CurrentAvatarState.address.ToHex()}]";
            var dummyAgentState = new AgentState(new Address());
            return AuraSummon.SimulateSummon(
                    addressHex, dummyAgentState,
                    tableSheets.EquipmentItemRecipeSheet,
                    tableSheets.EquipmentItemSheet,
                    tableSheets.EquipmentItemSubRecipeSheetV2,
                    tableSheets.EquipmentItemOptionSheet,
                    tableSheets.SkillSheet,
                    summonRow, summonCount, random, blockIndex)
                .Select(tuple => tuple.Item2)
                .OrderByDescending(row => row.Grade)
                .ToList();
        }

        private void SetMaterialAssets()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            var headerMenu = Find<HeaderMenuStatic>();
            for (var i = 0; i < SummonGroup; i++)
            {
                var material = (CostType)_summonRows[i].CostMaterial;
                headerMenu.SetMaterial(i, material);
            }
        }

        public static void ButtonSubscribe(SimpleCostButton[] buttons, GameObject gameObject)
        {
            foreach (var button in buttons)
            {
                button.OnClickDisabledSubject.Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_SUMMONING"),
                        NotificationCell.NotificationType.Information)
                ).AddTo(gameObject);
            }

            LoadingHelper.Summon.Subscribe(tuple =>
            {
                var summoning = tuple != null;
                var state = summoning
                    ? ConditionalButton.State.Disabled
                    : ConditionalButton.State.Normal;

                foreach (var button in buttons)
                {
                    button.SetState(state);
                    var loading = false;
                    if (summoning)
                    {
                        var (material, totalCost) = tuple;
                        var cost = button.GetCostParam;
                        loading = material == (int)cost.type && totalCost == cost.cost;
                    }

                    button.Loading = loading;
                }
            }).AddTo(gameObject);
        }

        public static void ButtonSubscribe(
            SimpleCostButton button, SummonSheet.Row summonRow, int summonCount,
            List<IDisposable> disposables)
        {
            var costType = (CostType)summonRow.CostMaterial;
            var cost = summonRow.CostMaterialCount * summonCount;

            button.SetCost(costType, cost);
            button.OnClickSubject.Subscribe(state =>
            {
                switch (state)
                {
                    case ConditionalButton.State.Normal:
                        Find<Summon>().AuraSummonAction(summonRow.GroupId, summonCount);
                        break;
                    case ConditionalButton.State.Conditional:
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                        Find<PaymentPopup>().ShowAttract(
                            costType,
                            cost.ToString(),
                            L10nManager.Localize("UI_SUMMON_MATERIAL_NOT_ENOUGH"),
                            L10nManager.Localize("UI_SHOP"),
                            GoToMarket);
#else
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("NOTIFICATION_MATERIAL_NOT_ENOUGH"),
                            NotificationCell.NotificationType.Information);
#endif
                        break;
                }
            }).AddTo(disposables);
        }

        // Do not use with Aura summon tutorial. this logic is fake.
        public void SetCostUIForTutorial()
        {
            var costButton = drawItems.FirstOrDefault()?.draw1Button;
            if (costButton != null)
            {
                costButton.SetFakeUI(CostType.SilverDust, 0);
            }
        }

        private static void GoToMarket()
        {
            Find<Summon>().Close(true);
            Find<SummonResultPopup>().Close(true);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            Find<MobileShop>().Show();
        }

        public static bool HasNotification
        {
            get
            {
                var result = false;
                var summonSheet = Game.Game.instance.TableSheets.SummonSheet;
                foreach (var summonRow in summonSheet)
                {
                    foreach (var count in Counts)
                    {
                        var costType = (CostType)summonRow.CostMaterial;
                        var cost = summonRow.CostMaterialCount * count;
                        result |= SimpleCostButton.CheckCostOfType(costType, cost);
                    }
                }

                return result;
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickNormal1SummonButton()
        {
            var summonRow = Game.Game.instance.TableSheets.SummonSheet.First;
            var resultEquipment =
                States.Instance.CurrentAvatarState.inventory.Equipments.FirstOrDefault(e =>
                    e is Aura);
            var button = drawItems[0].draw1Button;
            if (resultEquipment is null)
            {
                button.OnClickSubject.OnNext(button.CurrentState.Value);
                return;
            }

            Find<SummonResultPopup>().Show(summonRow, 1, new List<Equipment> {resultEquipment},
                () =>
                {
                    Game.Game.instance.Stage.TutorialController.Play(50005);
                });

            // UI Reset for SetCostUIForTutorial() invoking
            button.UpdateObjects();
        }
    }
}

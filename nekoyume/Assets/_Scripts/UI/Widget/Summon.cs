using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    using Toggle = Module.Toggle;
    public class Summon : Widget
    {
        [Serializable]
        private class SummonInfo
        {
            public int summonSheetId;
            public Toggle tabToggle;
            public GameObject[] enableObj;
        }

        [Serializable]
        private class SummonItem
        {
            public Button infoButton;
            public TextMeshProUGUI nameText;
            public SimpleCostButton draw1Button;
            public SimpleCostButton draw10Button;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private SummonInfo[] summonInfos;
        [SerializeField] private SummonItem summonItem;
        [SerializeField] private Button skillInfoButton;

        private SummonSheet.Row[] _summonRows;
        private bool _isInitialized;
        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            });
            CloseWidget = closeButton.onClick.Invoke;

            for (var i = 0; i < summonInfos.Length; i++)
            {
                var index = i;
                summonInfos[i].tabToggle.onClickToggle.AddListener(() => SetSummonInfo(index));
            }

            ButtonSubscribe(summonItem.draw1Button, gameObject);
            ButtonSubscribe(summonItem.draw10Button, gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            closeButton.interactable = true;

            if (!_isInitialized)
            {
                _isInitialized = true;

                var summonSheet = Game.Game.instance.TableSheets.SummonSheet;
                _summonRows = summonInfos.Select(obj => summonSheet[obj.summonSheetId]).ToArray();
            }

            summonInfos[0].tabToggle.isOn = true;
            SetSummonInfo(0);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            SetMaterialAssets();
        }

        private void SetSummonInfo(int index)
        {
            foreach (var info in summonInfos)
            {
                foreach (var obj in info.enableObj)
                {
                    obj.SetActive(false);
                }
            }

            var currentInfo = summonInfos[index];
            foreach (var obj in currentInfo.enableObj)
            {
                obj.SetActive(true);
            }

            var summonRow = _summonRows[index];
            skillInfoButton.onClick.RemoveAllListeners();
            skillInfoButton.onClick.AddListener(() => { });

            _disposables.DisposeAllAndClear();

            summonItem.infoButton.OnClickAsObservable()
                .Subscribe(_ => { })
                .AddTo(_disposables);
            summonItem.nameText.text = summonRow.GetLocalizedName();

            ButtonSubscribe(summonItem.draw1Button, summonRow, 1, _disposables);
            ButtonSubscribe(summonItem.draw10Button, summonRow, 10, _disposables);
        }

        #region Action

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
            StartCoroutine(CoShowLoadingScreen(summonRow.Recipes.Select(r => r.Item1).ToList()));
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = null;
            SetMaterialAssets();

            summonItem.draw1Button.UpdateObjects();
            summonItem.draw10Button.UpdateObjects();

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

        #endregion

        private void SetMaterialAssets()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            var headerMenu = Find<HeaderMenuStatic>();
            var materials = _summonRows
                .Select(row => (CostType)row.CostMaterial)
                .Distinct().ToArray();

            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                headerMenu.SetMaterial(i, material);
            }
        }

        private IEnumerator CoShowLoadingScreen(List<int> recipes)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            IEnumerator CoChangeItem()
            {
                while (isActiveAndEnabled)
                {
                    foreach (var recipe in recipes)
                    {
                        loadingScreen.SpeechBubbleWithItem.SetItemMaterial(
                            new Item(ItemFactory.CreateItem(
                                TableSheets.Instance.EquipmentItemRecipeSheet[recipe]
                                    .GetResultEquipmentItemRow(),
                                new ActionRenderHandler.LocalRandom(0))), false);
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }

            loadingScreen.Show();
            loadingScreen.SetCloseAction(null);
            StartCoroutine(CoChangeItem());
            yield return new WaitForSeconds(.5f);

            loadingScreen.AnimateNPC(
                CombinationLoadingScreen.SpeechBubbleItemType.Aura,
                L10nManager.Localize("UI_COST_BLOCK", 1));
        }

        public static void ButtonSubscribe(SimpleCostButton button, GameObject gameObject)
        {
            button.OnClickDisabledSubject.Subscribe(_ =>
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_SUMMONING"),
                    NotificationCell.NotificationType.Information)
            ).AddTo(gameObject);

            LoadingHelper.Summon.Subscribe(tuple =>
            {
                var summoning = tuple != null;
                var state = summoning
                    ? ConditionalButton.State.Disabled
                    : ConditionalButton.State.Normal;

                button.SetState(state);
                var loading = false;
                if (summoning)
                {
                    // it will have to fix - rune has same material with aura
                    var (material, totalCost) = tuple;
                    var cost = button.GetCostParam;
                    loading = material == (int)cost.type && totalCost == cost.cost;
                }

                button.Loading = loading;
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
                        //  it will have to fix - summoning rune need another action
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
            var costButton = summonItem.draw1Button;
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
                    var costType = (CostType)summonRow.CostMaterial;
                    var cost = summonRow.CostMaterialCount;
                    result |= SimpleCostButton.CheckCostOfType(costType, cost);
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
            var button = summonItem.draw1Button;
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Blockchain;
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
            public CostType costType;
            public string nameEng;
            public string phrase1;
            public string phrase2;

            public SummonSheet.Row SummonSheetRow;
        }

        [Serializable]
        private class SummonItem
        {
            public Button infoButton;
            public TextMeshProUGUI nameText;
            public SummonCostButton draw1Button;
            public SummonCostButton draw10Button;
            public RectTransform backgroundRect;
            public TextMeshProUGUI phrase1Text;
            public GameObject phrase1Obj;
            public TextMeshProUGUI phrase2Text;
            public GameObject phrase2Obj;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private SummonInfo[] summonInfos;
        [SerializeField] private SummonItem summonItem;
        [SerializeField] private Button skillInfoButton;

        private bool _isInitialized;
        private readonly List<IDisposable> _disposables = new();

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

            foreach (var summonInfo in summonInfos)
            {
                summonInfo.tabToggle.onClickToggle.AddListener(() => SetSummonInfo(summonInfo));
                summonInfo.tabToggle.onClickObsoletedToggle.AddListener(() =>
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_COMING_SOON"),
                        NotificationCell.NotificationType.Information);
                });
            }

            LoadingHelper.Summon.Subscribe(_ => SetMaterialAssets()).AddTo(gameObject);

            summonItem.draw1Button.Subscribe(gameObject);
            summonItem.draw10Button.Subscribe(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            closeButton.interactable = true;

            if (!_isInitialized)
            {
                _isInitialized = true;

                var summonSheet = Game.Game.instance.TableSheets.SummonSheet;
                foreach (var summonInfo in summonInfos)
                {
                    summonInfo.SummonSheetRow =
                        summonSheet.TryGetValue(summonInfo.summonSheetId, out var row) ? row : null;
                }
            }

            summonInfos[0].tabToggle.isOn = true;
            SetSummonInfo(summonInfos[0]);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            SetMaterialAssets();
        }

        private void SetSummonInfo(SummonInfo currentInfo)
        {
            foreach (var info in summonInfos)
            {
                foreach (var obj in info.enableObj)
                {
                    obj.SetActive(false);
                }
            }

            foreach (var obj in currentInfo.enableObj)
            {
                obj.SetActive(true);
            }

            summonItem.backgroundRect
                .DOAnchorPosY(SummonUtil.GetBackGroundPosition(currentInfo.costType), .5f)
                .SetEase(Ease.InOutCubic);

            var enablePhrase1 = !string.IsNullOrEmpty(currentInfo.phrase1);
            summonItem.phrase1Obj.SetActive(enablePhrase1);
            summonItem.phrase1Text.text = enablePhrase1
                ? L10nManager.Localize(currentInfo.phrase1)
                : string.Empty;

            var enablePhrase2 = !string.IsNullOrEmpty(currentInfo.phrase2);
            summonItem.phrase2Obj.SetActive(enablePhrase2);
            summonItem.phrase2Text.text = enablePhrase2
                ? L10nManager.Localize(currentInfo.phrase2)
                : string.Empty;

            var summonRow = currentInfo.SummonSheetRow;
            skillInfoButton.onClick.RemoveAllListeners();
            skillInfoButton.onClick.AddListener(() => Find<SummonSkillsPopup>().Show(summonRow));

            _disposables.DisposeAllAndClear();

            summonItem.infoButton.OnClickAsObservable()
                .Subscribe(_ => Find<SummonDetailPopup>().Show(summonRow))
                .AddTo(_disposables);
            summonItem.nameText.text = currentInfo.nameEng;

            summonItem.draw1Button.Subscribe(summonRow, 1, GoToMarket, _disposables);
            summonItem.draw10Button.Subscribe(summonRow, 10, GoToMarket, _disposables);
        }

        #region Action

        public void SummonAction(int groupId, int summonCount)
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
                NcDebug.LogError($"Group : {groupId}, Material : {materialRow.GetLocalizedName()}, has :{count}.");
                return;
            }

            var firstRecipeId = summonRow.Recipes.First().Item1;
            if (tableSheets.EquipmentItemRecipeSheet.TryGetValue(firstRecipeId, out _))
            {
                ActionManager.Instance.AuraSummon(groupId, summonCount).Subscribe();
                StartCoroutine(CoShowAuraSummonLoadingScreen(summonRow.Recipes.Select(r => r.Item1).ToList()));
            }
            else if (tableSheets.RuneSheet.TryGetValue(firstRecipeId, out _))
            {
                ActionManager.Instance.RuneSummon(groupId, summonCount).Subscribe();
                StartCoroutine(CoShowRuneSummonLoadingScreen(summonRow.Recipes.Select(r => r.Item1).ToList()));
            }

            LoadingHelper.Summon.Value = new Tuple<int, int>(summonRow.CostMaterial, totalCost);
        }

        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = null;

            var summonRow = Game.Game.instance.TableSheets.SummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(summonRow, summonCount, random, eval.BlockIndex);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        public void OnActionRender(ActionEvaluation<RuneSummon> eval)
        {
            LoadingHelper.Summon.Value = null;

            var summonRow = Game.Game.instance.TableSheets.SummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateRune(summonRow, summonCount, random);
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

        private static List<FungibleAssetValue> SimulateRune(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random)
        {
            const int unit = RuneSummon.RuneQuantity;
            var simulateResult = RuneSummon.SimulateSummon(
                Game.Game.instance.TableSheets.RuneSheet, summonRow, summonCount, random);

            var result = new List<FungibleAssetValue>();
            foreach (var pair in simulateResult)
            {
                var quantity = pair.Value;
                while (quantity > 0)
                {
                    if (quantity >= unit)
                    {
                        result.Add(new FungibleAssetValue(pair.Key, unit, 0));
                        quantity -= unit;
                    }
                    else
                    {
                        result.Add(new FungibleAssetValue(pair.Key, quantity, 0));
                        quantity = 0;
                    }
                }
            }

            return result
                .OrderByDescending(rune => Util.GetTickerGrade(rune.Currency.Ticker))
                .ThenBy(rune =>
                    RuneFrontHelper.TryGetRuneData(rune.Currency.Ticker, out var runeData)
                        ? runeData.sortingOrder
                        : 0)
                .ToList();
        }

        #endregion

        public void SetMaterialAssets()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (!TryFind<HeaderMenuStatic>(out var headerMenu))
            {
                return;
            }

            var materials = summonInfos
                .Where(info => info.SummonSheetRow != null)
                .Select(info => (CostType)info.SummonSheetRow.CostMaterial)
                .Distinct().ToArray();

            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                headerMenu.SetMaterial(i, material);
            }
        }

        private IEnumerator CoShowAuraSummonLoadingScreen(List<int> recipes)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            IEnumerator CoChangeItem()
            {
                var equipmentRecipeSheet = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet;
                while (loadingScreen.isActiveAndEnabled)
                {
                    foreach (var recipe in recipes)
                    {
                        var equipment = ItemFactory.CreateItem(
                            equipmentRecipeSheet[recipe].GetResultEquipmentItemRow(),
                            new ActionRenderHandler.LocalRandom(0));
                        loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(equipment));

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
                L10nManager.Localize("UI_COST_BLOCK", 1),
                false);
        }

        private IEnumerator CoShowRuneSummonLoadingScreen(List<int> recipes)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            IEnumerator CoChangeItem()
            {
                var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                while (loadingScreen.isActiveAndEnabled)
                {
                    foreach (var recipe in recipes)
                    {
                        if (!runeSheet.TryGetValue(recipe, out var rune))
                        {
                            NcDebug.LogError($"Invalid recipe id: {recipe}");
                            continue;
                        }

                        var fav = new FungibleAssetValue(
                            Currencies.GetRune(rune.Ticker), 1, 0);
                        loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(fav));
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }

            loadingScreen.Show();
            loadingScreen.SetCloseAction(null);
            StartCoroutine(CoChangeItem());
            yield return new WaitForSeconds(.5f);

            loadingScreen.AnimateNPC(
                CombinationLoadingScreen.SpeechBubbleItemType.Rune,
                L10nManager.Localize("UI_COST_BLOCK", 1),
                false);
        }

        private static void GoToMarket()
        {
            Find<Summon>().Close(true);

            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            if(TryFind<MobileShop>(out var mobileShop))
            {
                mobileShop.Show();
            }
        }

        // Do not use with Aura summon tutorial. this logic is fake.
        public void SetCostUIForTutorial()
        {
            summonInfos[2].tabToggle.isOn = true;
            SetSummonInfo(summonInfos[2]);

            var costButton = summonItem.draw1Button;
            if (costButton != null)
            {
                costButton.SetFakeUI(CostType.SilverDust, 0);
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

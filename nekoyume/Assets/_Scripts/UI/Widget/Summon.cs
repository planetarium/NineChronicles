﻿using System;
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
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    using Toggle = Module.Toggle;
    public class Summon : Widget
    {
        [Serializable]
        public class SummonObject
        {
            public SummonResult summonResult;
            public Toggle tabToggle;
            public GameObject[] enableObj;
            public Color bottomImageColor;
        }

        [SerializeField]
        private Button infoButton;

        [SerializeField]
        private RectTransform backgroundRect;

        [SerializeField]
        private Image bottomImage;

        [SerializeField]
        private SummonCostButton[] costButtons;

        [SerializeField]
        private SummonObject[] summonObjects;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private RectTransform catRectTransform;

        private SummonObject _selectedSummonObj;
        private readonly List<IDisposable> _disposables = new();
        private int _selectedSummonCount = 10;

        protected override void Awake()
        {
            infoButton.OnClickAsObservable()
                .Subscribe(_ => Find<SummonProbabilityPopup>().Show(_selectedSummonObj.summonResult))
                .AddTo(gameObject);
            foreach (var summonObject in summonObjects)
            {
                summonObject.tabToggle.onClickToggle.AddListener(() =>
                {
                    OnClickSummonTabToggle(summonObject);
                });
            }

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
            });
            CloseWidget = closeButton.onClick.Invoke;
            foreach (var costButton in costButtons)
            {
                costButton.Subscribe(gameObject);
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            OnClickSummonTabToggle(summonObjects.First());
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
        }

        private void OnClickSummonTabToggle(SummonObject summonObject)
        {
            if (summonObject == _selectedSummonObj)
            {
                return;
            }

            if (_selectedSummonObj != null)
            {
                foreach (var obj in _selectedSummonObj.enableObj)
                {
                    obj.SetActive(false);
                }

                _selectedSummonObj.tabToggle.isOn = false;
            }

            _selectedSummonObj = summonObject;
            SetBySummonResult(summonObject.summonResult);
            bottomImage.color = summonObject.bottomImageColor;
            foreach (var obj in summonObject.enableObj)
            {
                obj.SetActive(true);
            }

            _selectedSummonObj.tabToggle.isOn = true;
        }

        private void SetBySummonResult(SummonResult resultType)
        {
            _disposables.DisposeAllAndClear();
            foreach (var costButton in costButtons)
            {
                costButton.gameObject.SetActive(false);
            }

            var rows = SummonFrontHelper.GetSummonRowsBySummonResult(resultType);
            // 실버 더스트를 안쓰는 소환의 경우, 못생긴 0번 버튼을 스킵하고 그 다음부터 선택한다.
            var index = rows.Any(row => (CostType)row.CostMaterial == CostType.SilverDust) ? 0 : 1;
            foreach (var (row, costMaterial, materialCount) in rows.Select(row => (row, row.CostMaterial, row.CostMaterialCount)))
            {
                var costButton = costButtons[index++];
                costButton.SetCost((CostType)costMaterial, materialCount);
                costButton.gameObject.SetActive(true);
                costButton.Subscribe(row, _selectedSummonCount, _disposables);
            }

            backgroundRect
                .DOAnchorPosY(SummonUtil.GetBackGroundPosition(resultType), .5f)
                .SetEase(Ease.InOutCubic);
            // 표시해야할 버튼이 2개 이하인 경우, 고양이 NPC의 위치를 밑으로 조금 내린다.
            var catPos = catRectTransform.anchoredPosition;
            catPos.y = rows.Count > 2 ? 0 : -120;
            catRectTransform.anchoredPosition = catPos;
        }

        public void SummonAction(SummonSheet.Row row)
        {
            // 어떤걸 뽑느냐에 따라 LoadingScreen 형태가 바뀐다.
            switch (_selectedSummonObj.summonResult)
            {
                case SummonResult.Aura:
                case SummonResult.Grimoire:
                    ActionManager.Instance.AuraSummon(row.GroupId, _selectedSummonCount);
                    StartCoroutine(CoShowAuraSummonLoadingScreen(row.Recipes.Select(r => r.Item1).ToList()));
                    break;
                case SummonResult.Rune:
                    ActionManager.Instance.RuneSummon(row.GroupId, _selectedSummonCount);
                    StartCoroutine(CoShowRuneSummonLoadingScreen(row.Recipes.Select(r => r.Item1).ToList()));
                    break;
                case SummonResult.FullCostume:
                case SummonResult.Title:
                    ActionManager.Instance.CostumeSummon(row.GroupId, _selectedSummonCount);
                    // TODO: 코스튬 뚱땅창 연출 추가해야함
                    break;
            }

            // 액션을 처리하는 동안 LoadingHelper에 사용중인 재화를 할당한다
            LoadingHelper.Summon.Value = new Tuple<int, int>(row.CostMaterial, row.CostMaterialCount * _selectedSummonCount);
        }

        // used in UGUI event
        public void OnCountToggleValueChanged(int count)
        {
            _selectedSummonCount = count;
            SetBySummonResult(_selectedSummonObj.summonResult);
        }

#region copy from old Summon.cs
        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = null;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            var summonRow = Game.Game.instance.TableSheets.EquipmentSummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(summonRow, summonCount, random, eval.BlockIndex);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        public void OnActionRender(ActionEvaluation<RuneSummon> eval)
        {
            LoadingHelper.Summon.Value = null;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            var summonRow = Game.Game.instance.TableSheets.RuneSummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateRune(summonRow, summonCount, random);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        public void OnActionRender(ActionEvaluation<CostumeSummon> eval)
        {
            LoadingHelper.Summon.Value = null;
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Summon);
            var summonRow = Game.Game.instance.TableSheets.CostumeSummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateCostume(summonRow, summonCount, random);
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
                .OrderBy(row => row.Grade)
                .ToList();
        }

        private static List<Costume> SimulateCostume(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var addressHex = $"[{States.Instance.CurrentAvatarState.address.ToHex()}]";
            return CostumeSummon.SimulateSummon(
                    addressHex,
                    tableSheets.CostumeItemSheet,
                    summonRow,
                    summonCount,
                    random)
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
                .OrderBy(rune => Util.GetTickerGrade(rune.Currency.Ticker))
                .ThenBy(rune =>
                    RuneFrontHelper.TryGetRuneData(rune.Currency.Ticker, out var runeData)
                        ? runeData.sortingOrder
                        : 0)
                .ToList();
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
#endregion
    }
}

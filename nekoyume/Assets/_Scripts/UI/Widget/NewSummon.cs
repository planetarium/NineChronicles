using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
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
    using Toggle = Module.Toggle;
    public class NewSummon : Widget
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
            // TODO: SummonDetailPopup도 새로 만들어야한다. 난 망했다
            infoButton.OnClickAsObservable()
                .Subscribe(_ => Find<SummoningProbabilityPopup>().Show(_selectedSummonObj.summonResult))
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
                costButton.Subscribe(row, _selectedSummonCount, null, _disposables);
            }

            backgroundRect
                .DOAnchorPosY(SummonUtil.GetBackGroundPosition(resultType), .5f)
                .SetEase(Ease.InOutCubic);
            var catPos = catRectTransform.anchoredPosition;
            catPos.y = rows.Count > 2 ? 0 : -120;
            catRectTransform.anchoredPosition = catPos;
        }

        public void SummonAction(SummonSheet.Row row)
        {
            switch (_selectedSummonObj.summonResult)
            {
                case SummonResult.Aura:
                case SummonResult.Grimoire:
                    ActionManager.Instance.AuraSummon(row.GroupId, _selectedSummonCount);
                    break;
                case SummonResult.Rune:
                    ActionManager.Instance.RuneSummon(row.GroupId, _selectedSummonCount);
                    break;
                case SummonResult.FullCostume:
                case SummonResult.Title:
                    ActionManager.Instance.CostumeSummon(row.GroupId, _selectedSummonCount);
                    break;
            }
        }

        public void OnCountToggleValueChanged(int count)
        {
            _selectedSummonCount = count;
            SetBySummonResult(_selectedSummonObj.summonResult);
        }

#region duplicated
        public void OnActionRender(ActionEvaluation<AuraSummon> eval)
        {
            LoadingHelper.Summon.Value = null;

            var summonRow = Game.Game.instance.TableSheets.EquipmentSummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateEquipment(summonRow, summonCount, random, eval.BlockIndex);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        public void OnActionRender(ActionEvaluation<RuneSummon> eval)
        {
            LoadingHelper.Summon.Value = null;

            var summonRow = Game.Game.instance.TableSheets.RuneSummonSheet[eval.Action.GroupId];
            var summonCount = eval.Action.SummonCount;
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var resultList = SimulateRune(summonRow, summonCount, random);
            Find<SummonResultPopup>().Show(summonRow, summonCount, resultList);
        }

        public void OnActionRender(ActionEvaluation<CostumeSummon> eval)
        {
            LoadingHelper.Summon.Value = null;

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
                .OrderByDescending(row => row.Grade)
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
                .OrderByDescending(rune => Util.GetTickerGrade(rune.Currency.Ticker))
                .ThenBy(rune =>
                    RuneFrontHelper.TryGetRuneData(rune.Currency.Ticker, out var runeData)
                        ? runeData.sortingOrder
                        : 0)
                .ToList();
        }
#endregion
    }
}

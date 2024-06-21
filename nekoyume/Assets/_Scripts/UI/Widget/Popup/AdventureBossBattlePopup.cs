using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Cysharp.Threading.Tasks;
    using Nekoyume.ActionExtensions;
    using Nekoyume.L10n;
    using Nekoyume.TableData.AdventureBoss;
    using System.Linq;
    using UniRx;

    public class AdventureBossBattlePopup : PopupWidget
    {
        [SerializeField] private GameObject[] challengeFloors;
        [SerializeField] private GameObject[] breakThroughFloors;
        [SerializeField] private TextMeshProUGUI challengeFloorText;
        [SerializeField] private TextMeshProUGUI breakThroughFloorText;
        [SerializeField] private ConditionalButton challengeButton;
        [SerializeField] private ConditionalButton breakThroughButton;
        [SerializeField] private BaseItemView[] firstClearItems;
        [SerializeField] private BaseItemView[] challengeRandomItems;
        [SerializeField] private BaseItemView[] breakThroughRandomItems;
        [SerializeField] private TextMeshProUGUI challengeApCostText;
        [SerializeField] private TextMeshProUGUI breakThroughApCostText;
        [SerializeField] private GameObject challengeLockObj;
        [SerializeField] private GameObject challengeContents;
        [SerializeField] private Button gotoUnlock;
        [SerializeField] private GameObject breakThroughContent;
        [SerializeField] private GameObject breakThroughNoContent;

        private long _challengeApPotionCost;
        private long _breakThroughApPotionCost;

        protected override void Awake()
        {
            base.Awake();
            challengeButton.OnClickSubject.Subscribe(_ =>
            {
                Find<AdventureBossPreparation>()
                    .Show(L10nManager.Localize("UI_ADVENTURE_BOSS_CHALLENGE"),
                        _challengeApPotionCost,
                        AdventureBossPreparation.AdventureBossPreparationType.Challenge);
                Close();
            }).AddTo(gameObject);
            breakThroughButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_BREAKTHROUGH"));
            breakThroughButton.OnClickSubject.Subscribe(_ =>
            {
                Find<AdventureBossPreparation>().Show(
                    L10nManager.Localize("UI_ADVENTURE_BOSS_BREAKTHROUGH"),
                    _breakThroughApPotionCost,
                    AdventureBossPreparation.AdventureBossPreparationType.BreakThrough);
                Close();
            }).AddTo(gameObject);
            gotoUnlock.onClick.AddListener(() =>
            {
                Close();
                var floor = Find<AdventureBoss>().CurrentUnlockFloor;
                if (floor != null)
                {
                    floor.OnClickUnlockAction();
                }
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var adventurebossData = Game.Game.instance.AdventureBossData;
            var seasonInfo = adventurebossData.SeasonInfo.Value;

            if (seasonInfo == null)
            {
                NcDebug.LogError("SeasonInfo is null");
                return;
            }

            var currentFloor = 0;
            var maxFloor = 5;

            if (adventurebossData.ExploreInfo.Value != null)
            {
                currentFloor = adventurebossData.ExploreInfo.Value.Floor;
                maxFloor = adventurebossData.ExploreInfo.Value.MaxFloor;
            }

            if (currentFloor >= maxFloor)
            {
                challengeLockObj.SetActive(true);
                challengeContents.SetActive(false);
                challengeFloorText.text = "";
            }
            else
            {
                challengeLockObj.SetActive(false);
                challengeContents.SetActive(true);
                challengeFloorText.text = $"{currentFloor + 1}F ~ {maxFloor}F";
            }

            if (currentFloor == 0)
            {
                breakThroughContent.SetActive(false);
                breakThroughNoContent.SetActive(true);
                breakThroughFloorText.text = "";
            }
            else
            {
                breakThroughContent.SetActive(true);
                breakThroughNoContent.SetActive(false);
                breakThroughFloorText.text = $"{1}F ~ {currentFloor}F";
            }

            for (int i = 0; i < challengeFloors.Length; i++)
            {
                breakThroughFloors[i].SetActive(i < currentFloor);
                challengeFloors[i].SetActive(i >= currentFloor && i < maxFloor);
            }

            if (!Game.Game.instance.AdventureBossData.GetCurrentBossData(out var bossData))
            {
                NcDebug.LogError("BossData is null");
                return;
            }

            _breakThroughApPotionCost = currentFloor * bossData.SweepAp;
            _challengeApPotionCost = (maxFloor - currentFloor) * bossData.ExploreAp;
            var currentApPotionCount =
                Game.Game.instance.States.CurrentAvatarState.inventory.GetMaterialCount(
                    (int)CostType.ApPotion);

            var breakThroughColorString = currentApPotionCount >= _breakThroughApPotionCost
                ? ""
                : "<color=#ff5d5d>";
            var challengeColorString =
                currentApPotionCount >= _challengeApPotionCost ? "" : "<color=#ff5d5d>";

            breakThroughApCostText.text = breakThroughColorString +
                                          L10nManager.Localize(
                                              "UI_ADVENTURE_BOSS_BATTLEPOPUP_AP_DESC",
                                              _breakThroughApPotionCost);
            challengeApCostText.text = challengeColorString +
                                       L10nManager.Localize("UI_ADVENTURE_BOSS_BATTLEPOPUP_AP_DESC",
                                           _challengeApPotionCost);

            var tableSheets = Game.Game.instance.TableSheets;

            var bossRow = tableSheets.AdventureBossSheet.Values.FirstOrDefault(row => row.BossId == seasonInfo.BossId);
            if (bossRow == null)
            {
                NcDebug.LogError($"BossSheet is not found. BossId: {seasonInfo.BossId}");
                return;
            }
            var challengeFloorRows = tableSheets.AdventureBossFloorSheet.Values
                .Where(row => row.AdventureBossId == bossRow.Id
                                && row.Floor > currentFloor
                                && row.Floor <= maxFloor);

            var challengeFloorFirstRewardDatas = tableSheets.AdventureBossFloorFirstRewardSheet.Values
                .Join(challengeFloorRows,
                      rewardRow => rewardRow.FloorId,
                      floorRow => floorRow.Id,
                      (rewardRow, floorRow) => new
                      {
                          rewardRow.Rewards
                      })
                .SelectMany(rewardRow => rewardRow.Rewards)
                .GroupBy(r => r.ItemId)
                .Select(g => new AdventureBossSheet.RewardAmountData(
                    g.First().ItemType,
                    g.Key,
                    g.Sum(r => r.Amount)))
                .ToList();

            var challengeFloorRandomRewardDatas = challengeFloorRows
                .SelectMany(row => row.Rewards)
                .GroupBy(r => r.ItemId)
                .Select(g => new AdventureBossSheet.RewardAmountData(
                    g.First().ItemType,
                    g.Key,
                    g.Sum(r => r.Max)))
                .ToList();

            var breakthroughFloorRandomRewardDatas = tableSheets.AdventureBossFloorSheet.Values
                .Where(row => row.AdventureBossId == bossRow.Id
                                && row.Floor <= currentFloor)
                .SelectMany(row => row.Rewards)
                .GroupBy(r => r.ItemId)
                .Select(g => new AdventureBossSheet.RewardAmountData(
                    g.First().ItemType,
                    g.Key,
                    g.Sum(r => r.Max)))
                .ToList();

            for (int i = 0; i < firstClearItems.Length; i++)
            {
                if (i < challengeFloorFirstRewardDatas.Count)
                {
                    firstClearItems[i].gameObject.SetActive(true);
                    firstClearItems[i].ItemViewSetAdventureBossItemData(challengeFloorFirstRewardDatas[i]);
                }
                else
                {
                    firstClearItems[i].gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < challengeRandomItems.Length; i++)
            {
                if (i < challengeFloorRandomRewardDatas.Count)
                {
                    challengeRandomItems[i].gameObject.SetActive(true);
                    challengeRandomItems[i].ItemViewSetAdventureBossItemData(challengeFloorRandomRewardDatas[i]);
                }
                else
                {
                    challengeRandomItems[i].gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < breakThroughRandomItems.Length; i++)
            {
                if (i < breakthroughFloorRandomRewardDatas.Count)
                {
                    breakThroughRandomItems[i].gameObject.SetActive(true);
                    breakThroughRandomItems[i].ItemViewSetAdventureBossItemData(breakthroughFloorRandomRewardDatas[i]);
                }
                else
                {
                    breakThroughRandomItems[i].gameObject.SetActive(false);
                }
            }
            base.Show(ignoreShowAnimation);
        }
    }
}

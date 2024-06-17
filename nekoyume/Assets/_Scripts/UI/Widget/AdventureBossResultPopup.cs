using Libplanet.Types.Assets;
using Nekoyume.Data;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using DG.Tweening;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.TableData.AdventureBoss;
    using System.Linq;
    using UniRx;
    public class AdventureBossResultPopup : Widget
    {
        [SerializeField]
        private TextMeshProUGUI subTitle;
        [SerializeField]
        private TextMeshProUGUI scoreText;
        [SerializeField]
        private TextMeshProUGUI cumulativeScoreText;
        [SerializeField]
        private TextMeshProUGUI apPotionUsed;
        [SerializeField]
        private GameObject firstRewardGroup;
        [SerializeField]
        private SimpleCountableItemView[] firstRewardsItemView;
        [SerializeField]
        private SimpleCountableItemView[] secondRewardsItemView;
        [SerializeField]
        private GameObject[] clearEffect;

        private int _usedApPotion;

        public void SetData(int usedApPotion, int totalApPotion, int lastClearFloor, List<AdventureBossSheet.RewardAmountData> exploreRewards, List<AdventureBossSheet.RewardAmountData> firstRewards = null)
        {
            subTitle.text = L10nManager.Localize("ADVENTURE_BOSS_RESULT_SUB_TITLE", lastClearFloor.ToOrdinal());
            _usedApPotion = usedApPotion;
            apPotionUsed.text = L10nManager.Localize("ADVENTURE_BOSS_RESULT_AP_POTION_USED", usedApPotion, totalApPotion);
            if(firstRewards == null || firstRewards.Count == 0)
            {
                firstRewardGroup.SetActive(false);
            }
            else
            {
                firstRewardGroup.SetActive(true);
                RefreshItemView(firstRewards, firstRewardsItemView);
            }
            RefreshItemView(exploreRewards, secondRewardsItemView);
        }

        private void RefreshItemView(List<AdventureBossSheet.RewardAmountData> rewards, SimpleCountableItemView[] itemViews)
        {
            rewards = rewards.GroupBy(r => r.ItemId)
                            .Select(g => new AdventureBossSheet.RewardAmountData(
                                g.First().ItemType,
                                g.Key,
                                g.Sum(r => r.Amount)))
                            .OrderBy(r => r.ItemId).ToList();

            for (int i = 0; i < itemViews.Length; i++)
            {
                if (i < rewards.Count)
                {
                    switch (rewards[i].ItemType)
                    {
                        case "Material":
                            var countableItemMat = new CountableItem(
                            ItemFactory.CreateMaterial(TableSheets.Instance.MaterialItemSheet[rewards[i].ItemId]),
                            rewards[i].Amount);
                            itemViews[i].SetData(countableItemMat, () => ShowTooltip(countableItemMat.ItemBase.Value));
                            itemViews[i].gameObject.SetActive(true);
                            break;
                        case "Rune":
                            RuneSheet runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                            runeSheet.TryGetValue(rewards[i].ItemId, out var runeRow);
                            if (runeRow != null)
                            {
                                itemViews[i].SetData(GetFavCountableItem(runeRow.Ticker, rewards[i].Amount));
                                itemViews[i].gameObject.SetActive(true);
                            }
                            else
                            {
                                itemViews[i].gameObject.SetActive(false);
                            }
                            break;
                        case "Crystal":
                            itemViews[i].SetData(GetFavCountableItem("CRYSTAL", rewards[i].Amount));
                            itemViews[i].gameObject.SetActive(true);
                            break;
                        default:
                            itemViews[i].gameObject.SetActive(false);
                            break;
                    }
                }
                else
                {
                    itemViews[i].gameObject.SetActive(false);
                }
            }
        }

        private static CountableItem GetFavCountableItem(string ticker,int amount)
        {
            var currency = Currency.Legacy(ticker, 0, null);
            var fav = new FungibleAssetValue(currency, amount, 0);
            var countableItemRune = new CountableItem(fav, amount);
            return countableItemRune;
        }

        private void ShowTooltip(ItemBase model)
        {
            AudioController.PlayClick();
            var tooltip = ItemTooltip.Find(model.ItemType);
            tooltip.Show(model, string.Empty, false, null);
        }

        public void Show(int score, bool ignoreShowAnimation = false)
        {
            foreach (var item in clearEffect)
            {
                item.SetActive(score > 0);
            }
            scoreText.text = score.ToString("N0");

            var totalScore = 0;
            var start = 0;
            if (Game.Game.instance.AdventureBossData.ExploreInfo.Value != null)
            {
                start = Game.Game.instance.AdventureBossData.ExploreInfo.Value.Score;
            }
            totalScore = start + score;

            DOTween.To(() => start, x => start = x, totalScore, 0.3f)
                .SetDelay(0.2f)
                .OnUpdate(() => cumulativeScoreText.text = start.ToString("N0"))
                .SetEase(Ease.InOutQuad);
            //cumulativeScoreText.text = Game.Game.instance.AdventureBossData.ExploreInfo.Value.Score.ToString("N0");

            base.Show(ignoreShowAnimation);
        }

        public void OnClickTower()
        {
            Close();
            var worldMapLoading = Find<LoadingScreen>();
            worldMapLoading.Show();

            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                var worldMap = Find<WorldMap>();
                worldMap.Show(States.Instance.CurrentAvatarState.worldInformation, true);
                Find<AdventureBoss>().Show();
                worldMapLoading.Close(true);
            });
        }

        public void OnClickBreakthrough()
        {
            Close();
            var worldMapLoading = Find<LoadingScreen>();
            worldMapLoading.Show();

            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                var worldMap = Find<WorldMap>();
                worldMap.Show(States.Instance.CurrentAvatarState.worldInformation, true);
                Find<AdventureBoss>().Show();
                var currentFloor = 0;
                if (Game.Game.instance.AdventureBossData.ExploreInfo.Value != null)
                {
                    currentFloor = Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor;
                }
                Find<AdventureBossPreparation>().Show(
                        L10nManager.Localize("UI_ADVENTURE_BOSS_BREAKTHROUGH"),
                        currentFloor * SweepAdventureBoss.UnitApPotion,
                        AdventureBossPreparation.AdventureBossPreparationType.BreakThrough);
                worldMapLoading.Close(true);
            });
        }
    }
}


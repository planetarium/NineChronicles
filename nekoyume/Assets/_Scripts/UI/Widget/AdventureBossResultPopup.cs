using Libplanet.Types.Assets;
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
        [SerializeField]
        private GameObject retryButton;

        private int _prevScore;
        private int _lastClearFloor;

        public void SetData(int usedApPotion, int totalApPotion, int lastClearFloor, int prevScore, List<AdventureBossSheet.RewardAmountData> exploreRewards, List<AdventureBossSheet.RewardAmountData> firstRewards = null)
        {
            _prevScore = prevScore;
            _lastClearFloor = lastClearFloor;
            subTitle.text = L10nManager.Localize("ADVENTURE_BOSS_RESULT_SUB_TITLE", lastClearFloor.ToOrdinal());
            apPotionUsed.text = L10nManager.Localize("ADVENTURE_BOSS_RESULT_AP_POTION_USED", usedApPotion, totalApPotion);
            if (firstRewards == null || firstRewards.Count == 0)
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

        private static CountableItem GetFavCountableItem(string ticker, int amount)
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
            var start = _prevScore;
            totalScore = start + score;

            if(totalScore == 0)
            {
                retryButton.SetActive(false);
            }

            cumulativeScoreText.text = start.ToString("N0");
            DOTween.To(() => start, x => start = x, totalScore, 0.3f)
                .SetDelay(1.1f)
                .OnUpdate(() => cumulativeScoreText.text = start.ToString("N0"))
                .SetEase(Ease.InOutQuad);

            AudioController.instance.PlayMusic(AudioController.MusicCode.PVPWin);
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

                //시즌이 종료된상황에서는 어드벤처보스ui가 안뜰가능성이 있음.
                if (!Find<AdventureBoss>().isActiveAndEnabled)
                {
                    NcDebug.LogError("AdventureBoss is not active");
                    return;
                }

                if (!Game.Game.instance.AdventureBossData.GetCurrentBossData(out var bossData))
                {
                    NcDebug.LogError("BossData is null");
                    return;
                }

                Find<AdventureBossPreparation>().Show(
                        L10nManager.Localize("UI_ADVENTURE_BOSS_BREAKTHROUGH"),
                        _lastClearFloor * bossData.SweepAp,
                        AdventureBossPreparation.AdventureBossPreparationType.BreakThrough);
                worldMapLoading.Close(true);
            });
        }
    }
}


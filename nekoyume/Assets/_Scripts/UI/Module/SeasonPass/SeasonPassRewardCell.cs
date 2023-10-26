using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine.UI;
using Nekoyume.Game.Controller;
using System.Numerics;

namespace Nekoyume.UI.Module
{

    public class SeasonPassRewardCell : MonoBehaviour
    {
        [Serializable]
        public class RewardCell
        {
            [SerializeField]
            public GameObject Root;
            [SerializeField]
            public GameObject Light;
            [SerializeField]
            public BaseItemView ItemView;
            [SerializeField]
            public Button TooltipButton;

            private IDisposable disposable;
            private ItemBase itemBaseForToolTip = null;

            public void SetData(SeasonPassServiceClient.ItemInfoSchema itemInfo, SeasonPassServiceClient.CurrencyInfoSchema currencyInfo, int level, bool isNormal)
            {
                ItemView.Container.SetActive(true);
                ItemView.EmptyObject.SetActive(false);
                ItemView.EnoughObject.SetActive(false);
                ItemView.MinusObject.SetActive(false);
                ItemView.ExpiredObject.SetActive(false);
                ItemView.SelectBaseItemObject.SetActive(false);
                ItemView.SelectMaterialItemObject.SetActive(false);
                ItemView.LockObject.SetActive(false);
                ItemView.ShadowObject.SetActive(false);
                ItemView.PriceText.gameObject.SetActive(false);
                ItemView.LoadingObject.SetActive(false);
                ItemView.EquippedObject.SetActive(false);
                ItemView.DimObject.SetActive(false);
                ItemView.TradableObject.SetActive(false);
                ItemView.SelectObject.SetActive(false);
                ItemView.FocusObject.SetActive(false);
                ItemView.NotificationObject.SetActive(false);
                ItemView.GrindingCountObject.SetActive((false));
                ItemView.LevelLimitObject.SetActive(false);

                disposable?.Dispose();

                if(itemInfo != null)
                {
                    Root.SetActive(true);
                    ItemView.ItemImage.overrideSprite = SpriteHelper.GetItemIcon(itemInfo.Id);
                    ItemView.CountText.text = $"x{itemInfo.Amount}";
                    try
                    {
                        var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[itemInfo.Id];
                        ItemView.GradeImage.sprite = SpriteHelper.GetItemBackground(itemSheetData.Grade);
                        
                        var dummyItem = ItemFactory.CreateItem(itemSheetData, new Cheat.DebugRandom());
                        itemBaseForToolTip = dummyItem;
                    }
                    catch
                    {
                        Debug.LogError($"Can't Find Item ID {itemInfo.Id} in ItemSheet");
                    }
                }
                else if(currencyInfo != null)
                {
                    Root.SetActive(true);
                    itemBaseForToolTip = null;
                    ItemView.ItemImage.overrideSprite = SpriteHelper.GetFavIcon(currencyInfo.Ticker);
                    ItemView.CountText.text = ((BigInteger)currencyInfo.Amount).ToCurrencyNotation();
                    ItemView.GradeImage.sprite = SpriteHelper.GetItemBackground(Util.GetTickerGrade(currencyInfo.Ticker));
                }
                else
                {
                    Root.SetActive(false);
                    itemBaseForToolTip = null;
                    return;
                }

                disposable = Game.Game.instance.SeasonPassServiceManager.AvatarInfo.Subscribe((avatarInfo) =>
                {
                    if (avatarInfo == null)
                        return;

                    int lastClaim = isNormal ? avatarInfo.LastNormalClaim : avatarInfo.LastPremiumClaim;
                    bool isUnrReceived = level > lastClaim && level <= avatarInfo.Level;
                    Light.SetActive(isUnrReceived);
                    ItemView.LevelLimitObject.SetActive(level > avatarInfo.Level);
                });

                if (TooltipButton.onClick.GetPersistentEventCount() < 1)
                {
                    TooltipButton.onClick.AddListener(() =>
                    {
                        if (itemBaseForToolTip == null)
                            return;

                        AudioController.PlayClick();
                        var tooltip = ItemTooltip.Find(itemBaseForToolTip.ItemType);
                        tooltip.Show(itemBaseForToolTip, string.Empty, false, null);
                    });
                }
            }
        }

        [SerializeField]
        private TextMeshProUGUI[] levels;

        [SerializeField]
        private GameObject levelNormal;

        [SerializeField]
        private GameObject levelLast;

        [SerializeField]
        private GameObject levelReceived;

        [SerializeField]
        private RewardCell normal;

        [SerializeField]
        private RewardCell[] premiums;

        [SerializeField]
        private GameObject Light;

        private SeasonPassServiceClient.RewardSchema rewardSchema;

        public void Awake()
        {
            Game.Game.instance.SeasonPassServiceManager.AvatarInfo.Subscribe((avatarInfo) =>
            {
                RefreshLevelLight(avatarInfo);
            });
        }

        private void RefreshLevelLight(SeasonPassServiceClient.UserSeasonPassSchema avatarInfo)
        {
            if (avatarInfo == null || rewardSchema == null)
                return;

            bool isUnrReceived = rewardSchema.Level > avatarInfo.LastNormalClaim && rewardSchema.Level <= avatarInfo.Level;
            Light.SetActive(isUnrReceived);
            levelLast.SetActive(isUnrReceived);
            levelNormal.SetActive(rewardSchema.Level > avatarInfo.Level);
            levelReceived.SetActive(rewardSchema.Level <= avatarInfo.LastNormalClaim);
        }

        public void SetData(SeasonPassServiceClient.RewardSchema reward)
        {
            rewardSchema = reward;

            foreach (var item in levels)
            {
                item.text = rewardSchema.Level.ToString();
            }

            RefreshLevelLight(Game.Game.instance.SeasonPassServiceManager.AvatarInfo.Value);

            normal.SetData(rewardSchema.Normal.Item.First(), rewardSchema.Normal.Currency.First(), rewardSchema.Level, true);

            int index = 0;

            foreach (var item in rewardSchema.Premium.Item)
            {
                if(index > premiums.Length)
                {
                    Debug.LogError("[SeasonPassRewardCell] out of range premiums item");
                    continue;
                }
                premiums[index].SetData(item, null, rewardSchema.Level, false);
                index++;
            }

            foreach (var item in rewardSchema.Premium.Currency)
            {
                if (index > premiums.Length)
                {
                    Debug.LogError("[SeasonPassRewardCell] out of range premiums currency");
                    continue;
                }
                premiums[index].SetData(null, item, rewardSchema.Level, false);
                index++;
            }

            for (; index < premiums.Length; index++)
            {
                premiums[index].SetData(null, null, rewardSchema.Level, false);
            }
        }
    }
}

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
using DG.Tweening;
using Nekoyume.Model.Mail;
using Nekoyume.L10n;
using Cysharp.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.UI.Scroller;

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

            private bool isNotPremium = false;
            private IDisposable disposable;
            private ItemBase itemBaseForToolTip = null;

            public void SetData(SeasonPassServiceClient.ItemInfoSchema itemInfo, SeasonPassServiceClient.CurrencyInfoSchema currencyInfo, int level, bool isNormal, SeasonPassServiceClient.PassType passType)
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
                ItemView.GrindingCountObject.SetActive(false);
                ItemView.LevelLimitObject.SetActive(false);
                ItemView.RewardReceived.SetActive(false);
                ItemView.RuneNotificationObj.SetActiveSafe(false);
                ItemView.RuneSelectMove.SetActive(false);
                ItemView.SelectCollectionObject.SetActive(false);
                ItemView.SelectArrowObject.SetActive(false);

                disposable?.Dispose();

                if (itemInfo != null)
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
                        NcDebug.LogError($"Can't Find Item ID {itemInfo.Id} in ItemSheet");
                    }
                }
                else if (currencyInfo != null)
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

                ApiClients.Instance.SeasonPassServiceManager.UserSeasonPassDatas.TryGetValue(passType, out var userSeasonPassData);
                if (userSeasonPassData == null)
                {
                    return;
                }
                var lastClaim = isNormal ? userSeasonPassData.LastNormalClaim : userSeasonPassData.LastPremiumClaim;
                var isUnrReceived = level > lastClaim && level <= userSeasonPassData.Level;
                Light.SetActive(isUnrReceived);
                if (!userSeasonPassData.IsPremium && !isNormal)
                {
                    ItemView.LevelLimitObject.SetActive(true);
                    isNotPremium = true;
                }
                else
                {
                    ItemView.LevelLimitObject.SetActive(level > userSeasonPassData.Level);
                }

                ItemView.RewardReceived.SetActive(level <= lastClaim);
                TooltipButton.onClick.RemoveAllListeners();
                TooltipButton.onClick.AddListener(() =>
                {
                    AudioController.PlayClick();

                    if (userSeasonPassData.IsPremium ||
                        userSeasonPassData.IsPremiumPlus)
                    {
                        if (itemBaseForToolTip == null)
                        {
                            AudioController.PlayClick();
                            Widget.Find<FungibleAssetTooltip>().Show(currencyInfo.Ticker, ((BigInteger)currencyInfo.Amount).ToCurrencyNotation(), null);
                            return;
                        }
                        var tooltip = ItemTooltip.Find(itemBaseForToolTip.ItemType);
                        tooltip.Show(itemBaseForToolTip, string.Empty, false, null);
                        return;
                    }

                    if (ItemView.LevelLimitObject.activeSelf && isNotPremium)
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize("NOTIFICATION_SEASONPASS_PREMIUM_LIMIT_UNLOCK_GUIDE"),
                            NotificationCell.NotificationType.Notification);
                    }
                    else
                    {
                        if (itemBaseForToolTip == null)
                        {
                            AudioController.PlayClick();
                            Widget.Find<FungibleAssetTooltip>().Show(currencyInfo.Ticker, ((BigInteger)currencyInfo.Amount).ToCurrencyNotation(), null);
                            return;
                        }
                        var tooltip = ItemTooltip.Find(itemBaseForToolTip.ItemType);
                        tooltip.Show(itemBaseForToolTip, string.Empty, false, null);
                    }
                });
            }

            public void ShowTweening()
            {
                Root.transform.DOScale(UnityEngine.Vector3.one, 0.7f).SetEase(Ease.OutBack);
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

        [SerializeField]
        private Button ReceiveBtn;

        private SeasonPassServiceClient.RewardSchema rewardSchema;
        private SeasonPassServiceClient.PassType currentPassType;

        public void Awake()
        {
            ReceiveBtn.onClick.AddListener(() =>
            {
                ReceiveBtn.gameObject.SetActive(false);
                ApiClients.Instance.SeasonPassServiceManager.ReceiveAll(currentPassType,
                    (result) =>
                    {
                        OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_AND_WAIT_PLEASE"), NotificationCell.NotificationType.Notification);
                        ApiClients.Instance.SeasonPassServiceManager.AvatarStateRefreshAsync().AsUniTask().Forget();
                    },
                    (error) => { OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_SEASONPASS_REWARD_CLAIMED_FAIL"), NotificationCell.NotificationType.Notification); });
            });
        }

        private void RefreshWithAvatarInfo(SeasonPassServiceClient.UserSeasonPassSchema avatarInfo)
        {
            if (avatarInfo == null || rewardSchema == null)
            {
                return;
            }

            var isUnReceived = rewardSchema.Level > avatarInfo.LastNormalClaim && rewardSchema.Level <= avatarInfo.Level;
            Light?.SetActive(isUnReceived);
            var isUnReceivedPremium = rewardSchema.Level > avatarInfo.LastPremiumClaim && avatarInfo.IsPremium && rewardSchema.Level <= avatarInfo.Level;
            ReceiveBtn?.gameObject.SetActive(isUnReceived || isUnReceivedPremium);

            if (levelLast != null)
            {
                levelLast.SetActive(isUnReceived);
            }

            if (levelNormal != null)
            {
                levelNormal.SetActive(rewardSchema.Level > avatarInfo.Level);
            }

            if (levelReceived != null)
            {
                levelReceived.SetActive(rewardSchema.Level <= avatarInfo.LastNormalClaim);
            }
        }

        public void SetLevelText(int level)
        {
            foreach (var item in levels)
            {
                item.text = level.ToString();
            }
        }

        public void SetData(SeasonPassServiceClient.RewardSchema reward, SeasonPassServiceClient.PassType passType)
        {
            rewardSchema = reward;
            currentPassType = passType;

            SetLevelText(rewardSchema.Level);

            if(ApiClients.Instance.SeasonPassServiceManager.UserSeasonPassDatas.TryGetValue(passType, out var userSeasonPassData))
            {
                RefreshWithAvatarInfo(userSeasonPassData);
            }
            else
            {
                Debug.LogError("Can't find avatar info");
            }

            normal.SetData(rewardSchema.Normal.Item.Count > 0 ? rewardSchema.Normal.Item.First() : null,
                rewardSchema.Normal.Currency.Count > 0 ? rewardSchema.Normal.Currency.First() : null,
                rewardSchema.Level, true, passType);

            var index = 0;

            foreach (var item in rewardSchema.Premium.Item)
            {
                if (index > premiums.Length)
                {
                    NcDebug.LogError("[SeasonPassRewardCell] out of range premiums item");
                    continue;
                }

                premiums[index].SetData(item, null, rewardSchema.Level, false, passType);
                index++;
            }

            foreach (var item in rewardSchema.Premium.Currency)
            {
                if (index > premiums.Length)
                {
                    NcDebug.LogError("[SeasonPassRewardCell] out of range premiums currency");
                    continue;
                }

                premiums[index].SetData(null, item, rewardSchema.Level, false, passType);
                index++;
            }

            for (; index < premiums.Length; index++)
            {
                premiums[index].SetData(null, null, rewardSchema.Level, false, passType);
            }
        }

        public void SetTweeningStarting()
        {
            normal.Root.transform.DOKill();
            normal.Root.transform.localScale = UnityEngine.Vector3.zero;
            foreach (var item in premiums)
            {
                item.Root.transform.DOKill();
                item.Root.transform.localScale = UnityEngine.Vector3.zero;
            }
        }

        public void ShowTweening()
        {
            normal.ShowTweening();

            foreach (var item in premiums)
            {
                item.ShowTweening();
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopBuy : Widget
    {
        private const int NPCId = 300000;
        private static readonly Vector2 NPCPosition = new Vector2(2.76f, -1.2f);
        private NPC _npc;

        [SerializeField] private ShopBuyItems shopItems = null;
        [SerializeField] private ShopBuyBoard shopBuyBoard = null;
        [SerializeField] private Button sellButton = null;
        [SerializeField] private Canvas frontCanvas;

        // [SerializeField] private SpeechBubble speechBubble = null;

        private Model.Shop SharedModel { get; set; }

        [SerializeField] private List<ShopItemViewRow> itemViewItems;

        protected override void Awake()
        {
            var ratio = (float)Screen.height / (float)Screen.width;
            var count = Mathf.RoundToInt(10 * ratio) - 2;

            shopItems.Items.Clear();
            for (int i = 0; i < itemViewItems.Count; i++)
            {
                itemViewItems[i].gameObject.SetActive(i < count);
                if (i < count)
                {
                    shopItems.Items.AddRange(itemViewItems[i].shopItemView);
                }
            }

            base.Awake();
            SharedModel = new Model.Shop();
            CloseWidget = null;
            sellButton.onClick.AddListener(() =>
            {
                _npc?.gameObject.SetActive(false);
                Find<ItemCountAndPricePopup>().Close();
                base.Close(true);
                Find<ShopSell>().Show();
            });
        }

        public override void Initialize()
        {
            base.Initialize();

            shopItems.SharedModel.SelectedItemView
                .Subscribe(OnClickShopItem)
                .AddTo(gameObject);

            SharedModel.ItemCountAndPricePopup.Value.Item
                .Subscribe(SubscribeItemPopup)
                .AddTo(gameObject);

            shopBuyBoard.OnChangeBuyType.Subscribe(SetMultiplePurchase).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            AsyncShow();
        }

        private async void AsyncShow(bool ignoreShowAnimation = false)
        {
            Find<DataLoadingScreen>().Show();
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var task = Task.Run(() =>
            {
                ReactiveShopState.Initialize();
                return true;
            });

            var result = await task;
            if (result)
            {
                base.Show(ignoreShowAnimation);

                Find<BottomMenu>().Show(
                    UINavigator.NavigationType.Back,
                    SubscribeBackButtonClick,
                    true,
                    BottomMenu.ToggleableType.Mail,
                    BottomMenu.ToggleableType.Quest,
                    BottomMenu.ToggleableType.Chat,
                    BottomMenu.ToggleableType.IllustratedBook,
                    BottomMenu.ToggleableType.Character);

                AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
                SetMultiplePurchase(false);

                var go = Game.Game.instance.Stage.npcFactory.Create(
                    NPCId,
                    NPCPosition,
                    LayerType.InGameBackground,
                    3);
                _npc = go.GetComponent<NPC>();
                _npc.GetComponent<SortingGroup>().sortingLayerName = LayerType.UI.ToLayerName();
                _npc.GetComponent<SortingGroup>().sortingOrder = 11;
                _npc.SpineController.Appear();

                frontCanvas.sortingLayerName = LayerType.UI.ToLayerName();
                go.SetActive(true);
                shopItems.Show();

                Find<DataLoadingScreen>().Close();
            }

            // States.Instance.SetShopState(new ShopState(
            //     (Bencodex.Types.Dictionary) Game.Game.instance.Agent.GetState(Addresses.Shop)),
            //     shopItems.Items.Count);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            // ShowSpeech("SPEECH_SHOP_GREETING_", CharacterAnimation.Type.Greeting);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<ItemCountAndPricePopup>().Close();
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
            _npc?.gameObject.SetActive(false);
        }

        private void ShowTooltip(ShopItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();

            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                ButtonEnabledFuncForBuy,
                L10nManager.Localize("UI_BUY"),
                _ => ShowBuyPopup(tooltip.itemInformation.Model.item.Value as ShopItem),
                _ => shopItems.SharedModel.DeselectItemView());
        }

        private void ShowBuyPopup(ShopItem shopItem)
        {
            if (shopItem is null ||
                shopItem.Dimmed.Value)
            {
                return;
            }

            var price = shopItem.Price.Value.GetQuantityString();
            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"), 1, price);
            Find<TwoButtonPopup>().Show(content,
                L10nManager.Localize("UI_BUY"),
                L10nManager.Localize("UI_CANCEL"),
                (() => { Buy(shopItem); }));
        }

        private void SubscribeItemPopup(CountableItem data)
        {
            if (data is null)
            {
                Find<ItemCountAndPricePopup>().Close();
                return;
            }

            Find<ItemCountAndPricePopup>().Pop(SharedModel.ItemCountAndPricePopup.Value);
        }

        private void AddWishList(ShopItemView view)
        {
            shopBuyBoard.UpdateWishList(shopItems.SharedModel);
        }

        private void Buy(ShopItem shopItem)
        {
            var props = new Value
            {
                ["Price"] = shopItem.Price.Value.GetQuantityString(),
            };
            Mixpanel.Track("Unity/Buy", props);

            var purchaseInfos = new List<Buy.PurchaseInfo>()
            {
                new Buy.PurchaseInfo(shopItem.ProductId.Value,
                    shopItem.SellerAgentAddress.Value,
                    shopItem.SellerAvatarAddress.Value,
                    shopItem.ItemSubType.Value)
            };
            Game.Game.instance.ActionManager.Buy(purchaseInfos);
            ResponseBuy(shopItem);
        }

        private void SetMultiplePurchase(bool value)
        {
            shopItems.SharedModel.SetMultiplePurchase(value);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            if (shopItems.SharedModel.isMultiplePurchase && shopItems.SharedModel.wishItems.Count > 0)
            {
                Widget.Find<TwoButtonPopup>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    Close);
            }
            else
            {
                Close();
            }
        }

        private void Close()
        {
            Close(true);
            Game.Event.OnRoomEnter.Invoke(true);
        }

        private static bool ButtonEnabledFuncForBuy(CountableItem inventoryItem)
        {
            return inventoryItem is ShopItem shopItem &&
                   States.Instance.GoldBalanceState.Gold >= shopItem.Price.Value;
        }

        private void ResponseBuy(ShopItem shopItem)
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;

            var buyerAgentAddress = States.Instance.AgentState.address;
            var productId = shopItem.ProductId.Value;

            LocalLayerModifier.ModifyAgentGold(buyerAgentAddress, -shopItem.Price.Value);
            // try
            // {
            //     States.Instance.ShopState.Unregister(productId);
            // }
            // catch (FailedToUnregisterInShopStateException e)
            // {
            //     Debug.LogError(e.Message);
            // }
            shopItems.SharedModel.RemoveItemSubTypeProduct(productId);

            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
            var format = L10nManager.Localize("NOTIFICATION_BUY_START");
            Notification.Push(MailType.Auction,
                string.Format(format, shopItem.ItemBase.Value.GetLocalizedName()));
        }

        private void OnClickShopItem(ShopItemView view)
        {
            if (shopItems.SharedModel.isMultiplePurchase)
            {
                AddWishList(view);
            }
            else
            {
                ShowTooltip(view);
            }
        }

        // private void ShowSpeech(string key,
        //     CharacterAnimation.Type type = CharacterAnimation.Type.Emotion)
        // {
        //     if (type == CharacterAnimation.Type.Greeting)
        //     {
        //         _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
        //     }
        //     else
        //     {
        //         _npc.PlayAnimation(NPCAnimation.Type.Emotion_01);
        //     }
        //
        //     speechBubble.SetKey(key);
        //     StartCoroutine(speechBubble.CoShowText());
        // }
    }
}

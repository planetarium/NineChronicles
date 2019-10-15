using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class ShopTest : PlayModeTest
    {
        [UnityTest]
        public IEnumerator Sell()
        {
            miner = new MinerFixture("sell");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "sell";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Any());
            var createAvatarTx = Game.instance.agent.Transactions.First().Value;
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

//            var dialog = Widget.Find<Dialog>();
//            if (dialog.isActiveAndEnabled)
//            {
//                while (dialog.isActiveAndEnabled)
//                {
//                    dialog.Skip();
//                    yield return null;
//                }
//            }
            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Buy,w.SharedModel.State.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Sell,w.SharedModel.State.Value);
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.ItemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventory.Tooltip.isActiveAndEnabled);
            w.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.ItemCountAndPricePopup.isActiveAndEnabled);
            w.ItemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Count == 2);
            var sellTx = Game.instance.agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return miner.CoMine(sellTx);
            yield return new WaitUntil(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());

            //Check shop state
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);
            w.Close();

            //Sell Cancel
            w.Show();
            Assert.IsTrue(w.isActiveAndEnabled);
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Sell, w.SharedModel.State.Value);
            Assert.AreEqual(1, w.SharedModel.ShopItems.Value.RegisteredProducts.Count);
            var shopItem = w.SharedModel.ShopItems.Value.RegisteredProducts.First();
            ActionManager.instance.SellCancellation(shopItem.SellerAgentAddress.Value, shopItem.ProductId.Value);
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Count == 3);
            var cancelTx = Game.instance.agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return miner.CoMine(cancelTx);
            yield return new WaitWhile(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());
            Assert.IsEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
        }

        [UnityTest]
        public IEnumerator Buy()
        {
            miner = new MinerFixture("buy");
            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "buy";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Any());
            var createAvatarTx = Game.instance.agent.Transactions.Values.First();
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Buy,w.SharedModel.State.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Sell,w.SharedModel.State.Value);
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.ItemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventory.Tooltip.isActiveAndEnabled);
            w.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.ItemCountAndPricePopup.isActiveAndEnabled);
            w.ItemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Count == 2);
            var sellTx = Game.instance.agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return miner.CoMine(sellTx);
            yield return new WaitUntil(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());

            //Check shop state
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);

            w.Close();
            yield return new WaitForEndOfFrame();
            w.Show();
            yield return new WaitForEndOfFrame();
            //TODO 다른 주소에서 구매처리하도록 개선해야함
            Assert.IsNull(w.SharedModel.ShopItems.Value.RegisteredProducts.FirstOrDefault(i =>
                i.SellerAgentAddress.Value != States.Instance.agentState.Value.address));
        }

        [UnityTest]
        public IEnumerator BuyFail()
        {
            miner = new MinerFixture("buy_fail");
            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            Debug.Log(0);
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "buyFail";
            loginDetail.CreateClick();
            Debug.Log(1);
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Any());
            var createAvatarTx = Game.instance.agent.Transactions.Values.First();
            Debug.Log(2);
            yield return miner.CoMine(createAvatarTx);
            Debug.Log(3);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            Debug.Log(4);
            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Buy,w.SharedModel.State.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.StateType.Sell,w.SharedModel.State.Value);
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.ItemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventory.Tooltip.isActiveAndEnabled);
            w.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.ItemCountAndPricePopup.isActiveAndEnabled);
            w.ItemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Count == 2);
            Debug.Log(5);
            var sellTx = Game.instance.agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return miner.CoMine(sellTx);
            Debug.Log(6);
            yield return new WaitUntil(() => States.Instance.shopState.Value.items.Any());
            Debug.Log(7);

            //Check shop state
            Assert.IsFalse(w.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);

            //Buy
            w.Close();
            yield return new WaitForEndOfFrame();
            Debug.Log(8);
            w.Show();
            yield return new WaitForEndOfFrame();
            Debug.Log(9);

            var current = States.Instance.currentAvatarState.Value.inventory.Items.Count();
            var currentGold = States.Instance.agentState.Value.gold;

            //Check Buy.Execute
            var shopItem = w.SharedModel.ShopItems.Value.RegisteredProducts.First();
            ActionManager.instance.Buy(shopItem.SellerAgentAddress.Value, shopItem.SellerAvatarAddress.Value,
                shopItem.ProductId.Value);
            yield return new WaitUntil(() => Game.instance.agent.Transactions.Count == 3);
            Debug.Log(10);
            var invalidTx = Game.instance.agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return miner.CoMine(invalidTx);
            Debug.Log(11);
            Assert.IsNotEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
            Assert.AreEqual(current, States.Instance.currentAvatarState.Value.inventory.Items.Count());
            Assert.AreEqual(currentGold, States.Instance.agentState.Value.gold);
        }
    }
}

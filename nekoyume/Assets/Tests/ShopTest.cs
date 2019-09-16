using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.UI;
using NUnit.Framework;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class ShopTest
    {
        private MinerFixture _miner;

        [TearDown]
        public void TearDown()
        {
            _miner?.TearDown();
        }

        [UnityTest]
        public IEnumerator Sell()
        {
            _miner = new MinerFixture("sell");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "sell";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.First().Value;
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            var dialog = Widget.Find<Dialog>();
            yield return new WaitUntil(() => dialog.isActiveAndEnabled);
            while (dialog.isActiveAndEnabled)
            {
                dialog.Skip();
                yield return null;
            }
            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Buy,w.Model.state.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Sell,w.Model.state.Value);
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.itemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventoryAndItemInfo.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.itemCountAndPricePopup.isActiveAndEnabled);
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 2);
            var sellTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(sellTx);
            yield return new WaitUntil(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());

            //Check shop state
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);
            w.Close();

            //Sell Cancel
            w.Show();
            Assert.IsTrue(w.isActiveAndEnabled);
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Sell, w.Model.state.Value);
            Assert.AreEqual(1, w.Model.shopItems.Value.registeredProducts.Count);
            var shopItem = w.Model.shopItems.Value.registeredProducts.First();
            ActionManager.instance.SellCancellation(shopItem.sellerAgentAddress.Value, shopItem.productId.Value);
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 3);
            var cancelTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(cancelTx);
            yield return new WaitWhile(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());
            Assert.IsEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
            _miner.TearDown();
        }

        [UnityTest]
        public IEnumerator Buy()
        {
            _miner = new MinerFixture("buy");
            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "buy";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.Values.First();
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Buy,w.Model.state.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Sell,w.Model.state.Value);
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.itemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventoryAndItemInfo.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.itemCountAndPricePopup.isActiveAndEnabled);
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 2);
            var sellTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(sellTx);
            yield return new WaitUntil(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());

            //Check shop state
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);

            w.Close();
            yield return new WaitForEndOfFrame();
            w.Show();
            yield return new WaitForEndOfFrame();
            //TODO 다른 주소에서 구매처리하도록 개선해야함
            Assert.IsNull(w.Model.shopItems.Value.registeredProducts.FirstOrDefault(i =>
                i.sellerAgentAddress.Value != States.Instance.agentState.Value.address));
            _miner.TearDown();
        }

        [UnityTest]
        public IEnumerator BuyFail()
        {
            _miner = new MinerFixture("buy_fail");
            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.nameField.text = "buyFail";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.Values.First();
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            var dialog = Widget.Find<Dialog>();
            yield return new WaitUntil(() => dialog.isActiveAndEnabled);
            while (dialog.isActiveAndEnabled)
            {
                dialog.Skip();
                yield return null;
            }
            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Buy,w.Model.state.Value);

            //Switching Sell panel
            w.bottomMenu.switchSellButton.button.onClick.Invoke();
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Sell,w.Model.state.Value);
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(w.itemCountAndPricePopup.isActiveAndEnabled);

            //Sell
            var item = w.inventoryAndItemInfo.inventory.scrollerController.GetByIndex(0);
            item.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            Assert.IsTrue(w.itemCountAndPricePopup.isActiveAndEnabled);
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 2);
            var sellTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(sellTx);
            yield return new WaitUntil(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());

            //Check shop state
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.AreEqual(1,
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Count);

            //Buy
            w.Close();
            yield return new WaitForEndOfFrame();
            w.Show();
            yield return new WaitForEndOfFrame();

            Assert.IsEmpty(w.shopItems.data.products);
            var current = States.Instance.currentAvatarState.Value.inventory.Items.Count();
            var currentGold = States.Instance.agentState.Value.gold;

            //Check Buy.Execute
            var shopItem = w.Model.shopItems.Value.registeredProducts.First();
            ActionManager.instance.Buy(shopItem.sellerAgentAddress.Value, shopItem.sellerAvatarAddress.Value,
                shopItem.productId.Value);
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 3);
            var invalidTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(invalidTx);
            Assert.IsNotEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
            Assert.AreEqual(current, States.Instance.currentAvatarState.Value.inventory.Items.Count());
            Assert.AreEqual(currentGold, States.Instance.agentState.Value.gold);
            _miner.TearDown();
        }
    }
}

using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.UI;
using NUnit.Framework;
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
            Widget.Find<LoginDetail>().CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.First().Value;
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);

            var w = Widget.Find<Shop>();
            w.Show();

            //Check shop state
            Assert.IsTrue(w.isActiveAndEnabled);
            Assert.AreEqual(Nekoyume.UI.Model.Shop.State.Buy,w.Model.state.Value);

            //Switching Sell panel
            w.switchSellButton.onClick.Invoke();
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

            //Sell Cancel
            var shopItem = w.shopItems.items.First(i =>
                i.Model.sellerAgentAddress.Value == States.Instance.agentState.Value.address);
            shopItem.button.onClick.Invoke();
            yield return new WaitUntil(() => w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 3);
            var cancelTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(cancelTx);
            yield return new WaitWhile(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
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
            Widget.Find<LoginDetail>().CreateClick();
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
            w.switchSellButton.onClick.Invoke();
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
            var current = States.Instance.currentAvatarState.Value.inventory.Items.Count();
            w.switchBuyButton.onClick.Invoke();
            w.shopItems.refreshButton.onClick.Invoke();
            var shopItem = w.shopItems.items.First(i =>
                i.Model.sellerAgentAddress.Value != States.Instance.agentState.Value.address);
            shopItem.button.onClick.Invoke();
            yield return new WaitUntil(() => w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 3);
            var cancelTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(cancelTx);
            yield return new WaitWhile(() =>
                States.Instance.shopState.Value.items[States.Instance.agentState.Value.address].Any());
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsFalse(Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.IsEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
            Assert.Greater(States.Instance.currentAvatarState.Value.inventory.Items.Count(), current);
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
            Widget.Find<LoginDetail>().CreateClick();
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
            w.switchSellButton.onClick.Invoke();
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
            var current = States.Instance.currentAvatarState.Value.inventory.Items.Count();
            var currentGold = States.Instance.agentState.Value.gold;

            //Check Shop.products is empty
            w.switchBuyButton.onClick.Invoke();
            w.shopItems.refreshButton.onClick.Invoke();
            Assert.IsEmpty(w.shopItems.data.products);

            //Check Buy.Execute
            var shopItem = w.shopItems.items.First(i =>
                i.Model.sellerAgentAddress.Value == States.Instance.agentState.Value.address);
            shopItem.button.onClick.Invoke();
            yield return new WaitUntil(() => w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            w.inventoryAndItemInfo.inventory.Tooltip.submitButton.onClick.Invoke();
            w.itemCountAndPricePopup.submitButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count == 3);
            var invalidTx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(invalidTx);
            yield return new WaitWhile(() => Widget.Find<LoadingScreen>().isActiveAndEnabled);
            Assert.IsFalse(w.inventoryAndItemInfo.inventory.Tooltip.isActiveAndEnabled);
            Assert.IsNotEmpty(States.Instance.shopState.Value.items[States.Instance.agentState.Value.address]);
            Assert.AreEqual(current, States.Instance.currentAvatarState.Value.inventory.Items.Count());
            Assert.AreEqual(currentGold, States.Instance.agentState.Value.gold);
            _miner.TearDown();
        }
    }
}
